using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Site.Models.Gallery.Raw;

namespace Site.Services.Lightroom
{
    // Wraps https://lr.adobe.io/v2/. Registered as a typed HttpClient (DI-managed,
    // pooled connections) rather than `new HttpClient()` per call, since this proxies
    // many image requests unlike the low-traffic Particle integration.
    //
    // Confirmed empirically against a real catalog: every catalog-scoped resource
    // (albums, album assets, asset renditions) requires a "catalogs/{catalogId}/"
    // path prefix - the bare paths shown in the API's own relative "href" link values
    // 404 on their own. The catalog id is fetched once and cached, then prefixed onto
    // every subsequent request path, including "next" pagination links.
    public class LightroomApiClient
    {
        // Confirmed empirically - only these four sizes are exposed as rendition
        // links on album/asset resources (no 2560/fullsize).
        public static readonly HashSet<string> AllowedRenditionSizes = new()
        {
            "thumbnail2x", "640", "1280", "2048",
        };

        private const string ResponsePrefix = "while (1) {}";

        private readonly HttpClient _httpClient;
        private readonly LightroomTokenService _tokenService;
        private readonly string _clientId;

        private string _cachedCatalogId;

        public LightroomApiClient(HttpClient httpClient, LightroomTokenService tokenService, IOptions<LightroomConfig> config)
        {
            _httpClient = httpClient;
            _tokenService = tokenService;
            _clientId = config.Value.ClientId;
        }

        public async Task<string> GetCatalogIdAsync()
        {
            if (_cachedCatalogId != null)
                return _cachedCatalogId;

            var catalog = await GetJsonAsync<LightroomCatalog>("catalog");
            _cachedCatalogId = catalog.Id;
            return _cachedCatalogId;
        }

        public async Task<List<LightroomAlbum>> GetAllAlbumsAsync()
        {
            var catalogId = await GetCatalogIdAsync();
            var albums = new List<LightroomAlbum>();
            string next = $"catalogs/{catalogId}/albums";

            while (next != null)
            {
                var page = await GetJsonAsync<LightroomAlbumsResponse>(next);
                albums.AddRange(page.Resources);
                next = page.Links.TryGetValue("next", out var nextLink) ? ResolveCatalogRelative(catalogId, nextLink.Href) : null;
            }

            return albums;
        }

        public async Task<List<LightroomAlbumAsset>> GetAllAlbumAssetsAsync(string albumId)
        {
            var catalogId = await GetCatalogIdAsync();
            var assets = new List<LightroomAlbumAsset>();
            string next = $"catalogs/{catalogId}/albums/{albumId}/assets?embed=asset";

            while (next != null)
            {
                var page = await GetJsonAsync<LightroomAlbumAssetsResponse>(next);
                assets.AddRange(page.Resources);
                next = page.Links.TryGetValue("next", out var nextLink) ? ResolveCatalogRelative(catalogId, nextLink.Href) : null;
            }

            return assets;
        }

        public async Task<(Stream Content, string ContentType)> GetRenditionAsync(string assetId, string size)
        {
            if (!AllowedRenditionSizes.Contains(size))
                throw new ArgumentException($"Unsupported rendition size '{size}'.", nameof(size));

            var catalogId = await GetCatalogIdAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, $"catalogs/{catalogId}/assets/{assetId}/renditions/{size}");
            await AddAuthHeadersAsync(request);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            return (stream, contentType);
        }

        // Pagination "next" hrefs come back relative to the catalog's own base
        // (".../catalogs/{catalogId}/"), not the client's root base address - e.g.
        // "albums/{id}/assets?limit=...&embed=asset" rather than a fully catalog-
        // scoped path. Re-prefix defensively in case that ever changes.
        private static string ResolveCatalogRelative(string catalogId, string href)
        {
            var prefix = $"catalogs/{catalogId}/";
            return href.StartsWith(prefix) ? href : prefix + href;
        }

        private async Task<T> GetJsonAsync<T>(string relativeUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            await AddAuthHeadersAsync(request);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var text = await response.Content.ReadAsStringAsync();
            if (text.StartsWith(ResponsePrefix))
                text = text[ResponsePrefix.Length..].TrimStart();

            return JsonConvert.DeserializeObject<T>(text);
        }

        private async Task AddAuthHeadersAsync(HttpRequestMessage request)
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("X-API-Key", _clientId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Site.Models.Gallery;
using Site.Models.Gallery.Raw;
using Site.Services.Lightroom;

namespace Site.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer,QueryAuth")]
    [Route("api/gallery")]
    public class GalleryController : Controller
    {
        private const string AlbumsCacheKey = "gallery-albums-raw";
        private static readonly TimeSpan AlbumsCacheTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RenditionCacheTtl = TimeSpan.FromHours(6);

        private static readonly TimeSpan ImageTokenTtl = TimeSpan.FromHours(6);

        private const string OAuthAuthorizeUrl = "https://ims-na1.adobelogin.com/ims/authorize/v2";
        private const string OAuthScope = "openid,lr_partner_apis,offline_access";
        private const string OAuthStateCachePrefix = "gallery-oauth-state:";

        private readonly LightroomApiClient _lightroom;
        private readonly LightroomTokenService _tokenService;
        private readonly IMemoryCache _cache;
        private readonly HashSet<string> _excludedAlbumIds;
        private readonly byte[] _imageTokenSecret;
        private readonly string _lightroomClientId;

        public GalleryController(
            LightroomApiClient lightroom,
            LightroomTokenService tokenService,
            IMemoryCache cache,
            IOptions<GalleryConfig> galleryConfig,
            IOptions<LightroomConfig> lightroomConfig)
        {
            _lightroom = lightroom;
            _tokenService = tokenService;
            _cache = cache;
            _excludedAlbumIds = new HashSet<string>(galleryConfig.Value.ExcludedAlbumIds ?? Array.Empty<string>());
            _imageTokenSecret = Encoding.UTF8.GetBytes(galleryConfig.Value.ImageTokenSecret);
            _lightroomClientId = lightroomConfig.Value.ClientId;
        }

        [HttpGet, Route("status")]
        public async Task<GalleryStatusDto> Status()
        {
            if (!_tokenService.IsConnected)
                return new GalleryStatusDto { Connected = false, LightroomAuthExpiresInDays = -1 };

            await _tokenService.GetAccessTokenAsync();
            return new GalleryStatusDto
            {
                Connected = true,
                LightroomAuthExpiresInDays = _tokenService.RefreshTokenExpiresInDays ?? -1,
            };
        }

        // Kicks off the "renew Lightroom login" button: sends the browser to Adobe's
        // consent screen. [AllowAnonymous] because this is a top-level browser
        // navigation (not an HttpClient call), so it can't carry the app's own Bearer
        // token - same constraint as the image rendition endpoint. The real gate is
        // that completing this requires actually logging into Jonas's Adobe account.
        [AllowAnonymous]
        [HttpGet, Route("oauth/start")]
        public IActionResult OAuthStart()
        {
            var state = Guid.NewGuid().ToString("N");
            _cache.Set($"{OAuthStateCachePrefix}{state}", true, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Size = 1,
            });

            var redirectUri = BuildOAuthRedirectUri();
            var authorizeUrl = $"{OAuthAuthorizeUrl}" +
                $"?client_id={Uri.EscapeDataString(_lightroomClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(OAuthScope)}" +
                $"&state={state}";

            return Redirect(authorizeUrl);
        }

        [AllowAnonymous]
        [HttpGet, Route("oauth/callback")]
        public async Task<IActionResult> OAuthCallback([FromQuery] string code, [FromQuery] string state)
        {
            var stateKey = $"{OAuthStateCachePrefix}{state}";
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(code) || !_cache.TryGetValue(stateKey, out _))
                return Redirect("/gallery?reauth=error");

            _cache.Remove(stateKey);

            try
            {
                await _tokenService.ExchangeAuthorizationCodeAsync(code, BuildOAuthRedirectUri());
                return Redirect("/gallery?reauth=success");
            }
            catch
            {
                return Redirect("/gallery?reauth=error");
            }
        }

        private string BuildOAuthRedirectUri() => $"{Request.Scheme}://{Request.Host}/api/gallery/oauth/callback";

        [HttpGet, Route("tree")]
        public async Task<ActionResult<List<GalleryTreeNode>>> Tree()
        {
            if (!_tokenService.IsConnected)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var albums = await GetCachedAlbumsAsync();
            return await BuildTree(albums);
        }

        [HttpGet, Route("albums/{albumId}")]
        public async Task<ActionResult<AlbumDetailDto>> AlbumDetail(string albumId)
        {
            if (!_tokenService.IsConnected)
                return StatusCode(StatusCodes.Status503ServiceUnavailable);

            var albums = await GetCachedAlbumsAsync();
            var album = albums.FirstOrDefault(a => a.Id == albumId);
            if (album == null || _excludedAlbumIds.Contains(albumId))
                return NotFound();

            var assets = await _lightroom.GetAllAlbumAssetsAsync(albumId);

            return new AlbumDetailDto
            {
                AlbumId = album.Id,
                AlbumName = album.Payload.Name,
                Breadcrumbs = BuildBreadcrumbs(albums, album),
                Images = assets
                    .Where(a => a.Asset?.Subtype == "image" && !IsRejected(a.Asset))
                    .Select(a => ToImageDto(a.Asset))
                    .ToList(),
            };
        }

        // <img>/CSS background-image/PhotoSwipe all load this via plain browser
        // requests - they can't carry the Authorization: Bearer header Angular's
        // MsalInterceptor attaches to HttpClient calls. So this endpoint can't sit
        // behind the normal [Authorize] wall; instead the JSON endpoints above (which
        // ARE fetched via HttpClient and do carry the Bearer token) embed a signed,
        // time-limited token in every rendition URL they hand out, and this endpoint
        // validates that token itself. Only someone who already authenticated to get
        // the JSON can ever have a valid image URL, and each one expires.
        [AllowAnonymous]
        [HttpGet, Route("assets/{assetId}/rendition/{size}")]
        public async Task<IActionResult> Rendition(string assetId, string size, [FromQuery] string t)
        {
            if (!LightroomApiClient.AllowedRenditionSizes.Contains(size))
                return BadRequest();

            if (!ValidateImageToken(assetId, size, t))
                return Unauthorized();

            var cacheKey = $"rendition:{assetId}:{size}";
            if (!_cache.TryGetValue(cacheKey, out (byte[] Bytes, string ContentType) cached))
            {
                var (stream, contentType) = await _lightroom.GetRenditionAsync(assetId, size);
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);
                cached = (buffer.ToArray(), contentType);

                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions
                {
                    Size = cached.Bytes.Length,
                    SlidingExpiration = RenditionCacheTtl,
                });
            }

            Response.Headers["Cache-Control"] = "public, max-age=604800, immutable";
            return File(cached.Bytes, cached.ContentType);
        }

        private async Task<List<LightroomAlbum>> GetCachedAlbumsAsync() =>
            await _cache.GetOrCreateAsync(AlbumsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AlbumsCacheTtl;
                entry.Size = 1;
                return await _lightroom.GetAllAlbumsAsync();
            });

        private async Task<List<GalleryTreeNode>> BuildTree(List<LightroomAlbum> albums)
        {
            var byId = albums
                .Where(a => a.Subtype is "collection" or "collection_set")
                .Where(a => !_excludedAlbumIds.Contains(a.Id))
                .ToDictionary(a => a.Id);

            var nodes = byId.Values.ToDictionary(
                a => a.Id,
                a => new GalleryTreeNode
                {
                    Id = a.Id,
                    Name = a.Payload.Name,
                    Type = a.Subtype == "collection_set" ? "folder" : "album",
                });

            var roots = new List<GalleryTreeNode>();
            foreach (var album in byId.Values)
            {
                var node = nodes[album.Id];
                var parentId = album.Payload.Parent?.Id;
                if (parentId != null && nodes.TryGetValue(parentId, out var parentNode))
                    parentNode.Children.Add(node);
                else
                    roots.Add(node);
            }

            foreach (var album in byId.Values.Where(a => a.Payload.Cover?.Id != null))
                nodes[album.Id].CoverUrl = RenditionUrl(album.Payload.Cover.Id, "thumbnail2x");

            foreach (var node in nodes.Values.Where(n => n.Type == "folder"))
                node.CoverUrl ??= FindDescendantCover(node);

            var albumIds = byId.Values.Where(a => a.Subtype == "collection").Select(a => a.Id).ToList();
            var counts = await Task.WhenAll(albumIds.Select(async id => (Id: id, Count: await GetImageCountAsync(id))));
            foreach (var (id, count) in counts)
                nodes[id].ImageCount = count;

            foreach (var root in roots)
                ComputeFolderImageCount(root);

            return roots;
        }

        private static int ComputeFolderImageCount(GalleryTreeNode node)
        {
            if (node.Type == "album")
                return node.ImageCount;

            node.ImageCount = node.Children.Sum(ComputeFolderImageCount);
            return node.ImageCount;
        }

        private static List<BreadcrumbItemDto> BuildBreadcrumbs(List<LightroomAlbum> albums, LightroomAlbum album)
        {
            var byId = albums.ToDictionary(a => a.Id);
            var chain = new List<BreadcrumbItemDto>();
            var parentId = album.Payload.Parent?.Id;

            while (parentId != null && byId.TryGetValue(parentId, out var parent))
            {
                chain.Add(new BreadcrumbItemDto { Id = parent.Id, Name = parent.Payload.Name });
                parentId = parent.Payload.Parent?.Id;
            }

            chain.Reverse();
            return chain;
        }

        // The Lightroom API doesn't report an album's asset count anywhere short of
        // paging through its full asset list, so this is cached separately (and
        // longer) than the tree itself to keep repeated tree builds cheap.
        private async Task<int> GetImageCountAsync(string albumId) =>
            await _cache.GetOrCreateAsync($"gallery-imagecount:{albumId}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                entry.Size = 1;
                var assets = await _lightroom.GetAllAlbumAssetsAsync(albumId);
                return assets.Count(a => a.Asset?.Subtype == "image" && !IsRejected(a.Asset));
            });

        // Lightroom's Pick/Reject flag (P/U/X in Lightroom Classic) is synced under
        // asset.payload.reviews.{deviceId}.flag as "pick" or "reject" - confirmed by
        // diffing a rejected asset against its non-rejected sibling, since it isn't
        // documented and doesn't show up unless an asset has actually been flagged.
        // If any device recorded a reject, treat it as rejected.
        private static bool IsRejected(LightroomAsset asset) =>
            (asset.Payload?["reviews"] as JObject)?
                .Properties()
                .Any(p => (p.Value as JObject)?.Value<string>("flag") == "reject") ?? false;

        private static string FindDescendantCover(GalleryTreeNode node) =>
            node.Children
                .Select(c => c.CoverUrl ?? (c.Type == "folder" ? FindDescendantCover(c) : null))
                .FirstOrDefault(url => url != null);

        private const int FullRenditionMaxDimension = 2048;

        private AlbumImageDto ToImageDto(LightroomAsset asset)
        {
            var (width, height) = GetScaledDimensions(asset.Payload, FullRenditionMaxDimension);

            return new AlbumImageDto
            {
                Id = asset.Id,
                ThumbnailUrl = RenditionUrl(asset.Id, "640"),
                FullUrl = RenditionUrl(asset.Id, "2048"),
                Width = width,
                Height = height,
                Metadata = ExtractMetadata(asset.Payload),
            };
        }

        // PhotoSwipe needs the pixel size of the image behind FullUrl to size slides
        // without distortion. The API doesn't report a rendition's actual output size,
        // so this is derived by scaling the asset's real (post-crop) dimensions down to
        // fit within the rendition's max dimension, the same way Lightroom generates it.
        private static (int? Width, int? Height) GetScaledDimensions(JObject payload, int maxDimension)
        {
            var develop = payload?["develop"] as JObject;
            var width = develop?.Value<int?>("croppedWidth") ?? payload?.SelectToken("importSource.originalWidth")?.Value<int?>();
            var height = develop?.Value<int?>("croppedHeight") ?? payload?.SelectToken("importSource.originalHeight")?.Value<int?>();

            if (!width.HasValue || !height.HasValue || width <= 0 || height <= 0)
                return (null, null);

            var scale = Math.Min(1.0, (double)maxDimension / Math.Max(width.Value, height.Value));
            return ((int)Math.Round(width.Value * scale), (int)Math.Round(height.Value * scale));
        }

        private string RenditionUrl(string assetId, string size) =>
            $"/api/gallery/assets/{assetId}/rendition/{size}?t={GenerateImageToken(assetId, size)}";

        private string GenerateImageToken(string assetId, string size)
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(ImageTokenTtl).ToUnixTimeSeconds();
            var signature = Sign(assetId, size, expiresAt);
            return $"{expiresAt}.{signature}";
        }

        private bool ValidateImageToken(string assetId, string size, string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var parts = token.Split('.', 2);
            if (parts.Length != 2 || !long.TryParse(parts[0], out var expiresAt))
                return false;

            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt)
                return false;

            var expectedSignature = Sign(assetId, size, expiresAt);
            var actual = Encoding.UTF8.GetBytes(parts[1]);
            var expected = Encoding.UTF8.GetBytes(expectedSignature);

            return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        private string Sign(string assetId, string size, long expiresAt)
        {
            using var hmac = new HMACSHA256(_imageTokenSecret);
            var payload = Encoding.UTF8.GetBytes($"{assetId}:{size}:{expiresAt}");
            var hash = hmac.ComputeHash(payload);
            return Convert.ToBase64String(hash).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static PhotoMetadataDto ExtractMetadata(JObject payload)
        {
            var xmp = payload?["xmp"] as JObject;
            var exif = xmp?["exif"] as JObject;
            var tiff = xmp?["tiff"] as JObject;
            var aux = xmp?["aux"] as JObject;
            var dc = xmp?["dc"] as JObject;
            var location = payload?["location"] as JObject;

            return new PhotoMetadataDto
            {
                FileName = payload?.SelectToken("importSource.fileName")?.Value<string>(),
                CaptureDate = ParseDate(payload?.Value<string>("captureDate")),
                CameraMake = tiff?.Value<string>("Make"),
                CameraModel = tiff?.Value<string>("Model"),
                Lens = aux?.Value<string>("Lens"),
                Aperture = FormatAperture(exif?["FNumber"]),
                ShutterSpeed = FormatShutterSpeed(exif?["ExposureTime"]),
                Iso = exif?.Value<int?>("ISOSpeedRatings"),
                FocalLength = FormatFocalLength(exif?["FocalLength"]),
                FlashFired = exif?.Value<bool?>("FlashFired"),
                Latitude = location?.Value<double?>("latitude"),
                Longitude = location?.Value<double?>("longitude"),
                City = location?.Value<string>("city"),
                State = location?.Value<string>("state"),
                Country = location?.Value<string>("country"),
                Title = dc?.Value<string>("title"),
                Description = dc?.Value<string>("description"),
                Rights = string.IsNullOrEmpty(dc?.Value<string>("rights")) ? null : dc.Value<string>("rights"),
                Creators = dc?["creator"]?.ToObject<List<string>>() ?? new List<string>(),
                Keywords = (dc?["subject"] as JObject)?.Properties().Select(p => p.Name).ToList() ?? new List<string>(),
            };
        }

        private static DateTimeOffset? ParseDate(string value) =>
            DateTimeOffset.TryParse(value, out var parsed) ? parsed : null;

        private static double? ParseRational(JToken token)
        {
            if (token is not JArray { Count: 2 } array)
                return null;

            var numerator = array[0].Value<double>();
            var denominator = array[1].Value<double>();
            return denominator == 0 ? null : numerator / denominator;
        }

        private static string FormatAperture(JToken token)
        {
            var value = ParseRational(token);
            return value.HasValue ? $"f/{value.Value:0.#}" : null;
        }

        private static string FormatShutterSpeed(JToken token)
        {
            var value = ParseRational(token);
            if (!value.HasValue || value.Value <= 0)
                return null;

            return value.Value >= 1
                ? $"{value.Value:0.#}s"
                : $"1/{Math.Round(1 / value.Value):0}s";
        }

        private static string FormatFocalLength(JToken token)
        {
            var value = ParseRational(token);
            return value.HasValue ? $"{value.Value:0}mm" : null;
        }
    }

    public class GalleryConfig
    {
        public string[] ExcludedAlbumIds { get; set; }

        // Signs the short-lived image-access tokens embedded in rendition URLs -
        // see the "signed URL" comment on GalleryController.RenditionUrl.
        public string ImageTokenSecret { get; set; }
    }
}

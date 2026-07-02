using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Site.Services.Lightroom
{
    // Adobe IMS access tokens are short-lived (~1h) and can be refreshed silently at
    // any time. The refresh token itself does NOT rotate and does NOT extend on use -
    // it hard-expires ~14 days after the interactive login that created it (Adobe's
    // documented default for OAuth Web App credentials, confirmed empirically). There
    // is no way to renew it without a human completing the browser consent flow again
    // (see tools/lightroom-oauth-bootstrap), so this service tracks and exposes that
    // expiry rather than pretending it can keep itself alive forever.
    public class LightroomTokenService
    {
        private const string TokenUrl = "https://ims-na1.adobelogin.com/ims/token/v3";

        private readonly IOptions<LightroomConfig> _config;
        private readonly LightroomRefreshTokenStore _refreshTokenStore;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlimLock _lock = new();

        private string _refreshToken;
        private string _accessToken;
        private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;
        private DateTimeOffset? _refreshTokenExpiresAt;

        public LightroomTokenService(
            IOptions<LightroomConfig> config,
            LightroomRefreshTokenStore refreshTokenStore,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _refreshTokenStore = refreshTokenStore;
            _refreshToken = refreshTokenStore.TryRead();
            _httpClient = httpClientFactory.CreateClient(nameof(LightroomTokenService));
        }

        // False until the "Anslut Lightroom" / "Förnya nu" button has been used at
        // least once - secrets.json only ever holds the client id/secret, never a
        // refresh token, so there's nothing to fall back to before that.
        public bool IsConnected => !string.IsNullOrEmpty(_refreshToken);

        // Called by GalleryController's OAuth callback once a re-auth via the UI
        // button completes. Persists the new refresh token and forces the next
        // GetAccessTokenAsync() call to use it immediately.
        public void SetRefreshToken(string refreshToken)
        {
            _refreshToken = refreshToken;
            _refreshTokenStore.Write(refreshToken);
            _accessToken = null;
            _accessTokenExpiresAt = DateTimeOffset.MinValue;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (!IsConnected)
                throw new LightroomNotConnectedException();

            if (IsAccessTokenValid())
                return _accessToken;

            using (await _lock.AcquireAsync())
            {
                if (IsAccessTokenValid())
                    return _accessToken;

                await RefreshAsync();
                return _accessToken;
            }
        }

        // Null until the first refresh has happened (e.g. before first request since app start).
        public int? RefreshTokenExpiresInDays =>
            _refreshTokenExpiresAt.HasValue
                ? (int)Math.Ceiling((_refreshTokenExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays)
                : null;

        // Used by GalleryController's OAuth callback (the in-app "renew" button) to
        // trade a fresh authorization code for a new refresh token, replacing the one
        // that's about to (or already did) expire.
        public async Task<string> ExchangeAuthorizationCodeAsync(string code, string redirectUri)
        {
            var config = _config.Value;
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);
            var refreshToken = json.Value<string>("refresh_token");

            if (string.IsNullOrEmpty(refreshToken))
                throw new InvalidOperationException("Adobe did not return a refresh_token - check the offline_access scope was granted.");

            SetRefreshToken(refreshToken);
            return refreshToken;
        }

        private bool IsAccessTokenValid() =>
            _accessToken != null && DateTimeOffset.UtcNow < _accessTokenExpiresAt - TimeSpan.FromMinutes(5);

        private async Task RefreshAsync()
        {
            var config = _config.Value;
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = _refreshToken,
                }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(body);

            _accessToken = json.Value<string>("access_token");
            var expiresInSeconds = json.Value<double?>("expires_in") ?? 3600;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
            _refreshTokenExpiresAt = ExtractRefreshTokenExpiry(_accessToken) ?? _refreshTokenExpiresAt;
        }

        // The access token is a JWT carrying an "rtea" (refresh-token-expires-at) claim,
        // epoch milliseconds. Decoded manually (no JWT library) since this is the only
        // claim we need.
        private static DateTimeOffset? ExtractRefreshTokenExpiry(string accessToken)
        {
            try
            {
                var parts = accessToken.Split('.');
                if (parts.Length < 2)
                    return null;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var payloadJson = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
                var rtea = payloadJson.Value<string>("rtea");

                return rtea != null && long.TryParse(rtea, out var rteaMs)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(rteaMs)
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    // Minimal async-friendly mutex wrapper around SemaphoreSlim(1,1).
    internal class SemaphoreSlimLock
    {
        private readonly System.Threading.SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IDisposable> AcquireAsync()
        {
            await _semaphore.WaitAsync();
            return new Releaser(_semaphore);
        }

        private class Releaser : IDisposable
        {
            private readonly System.Threading.SemaphoreSlim _semaphore;
            public Releaser(System.Threading.SemaphoreSlim semaphore) => _semaphore = semaphore;
            public void Dispose() => _semaphore.Release();
        }
    }

    public class LightroomConfig
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class LightroomNotConnectedException : Exception
    {
        public LightroomNotConnectedException()
            : base("Lightroom is not connected yet - use the \"Anslut Lightroom\" button.")
        {
        }
    }
}

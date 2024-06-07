using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Site
{
    public class QueryStringAuthOptions : AuthenticationSchemeOptions
    {
        public string QueryStringKey { get; set; }
        public Dictionary<string, string> Devices { get; set; } = new();

        internal void AddDevice(string deviceName)
        {
            Devices.Add(CreateHash(deviceName), deviceName);
        }

        private static string CreateHash(string deviceName)
        {
            using var md5 = MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(deviceName);
            var hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes).ToLower();
        }
    }

    public static class QueryStringAuthDefaults
    {
        public const string SchemaName = "QueryAuth";
    }

    public class QueryStringAuth : AuthenticationHandler<QueryStringAuthOptions>
    {
        public QueryStringAuth(
            IOptionsMonitor<QueryStringAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected string ParseApiKey()
        {
            if (Request.Query.TryGetValue(Options.QueryStringKey, out var value))
                return value.FirstOrDefault();

            return string.Empty;
        }

        static ClaimsPrincipal BuildPrincipal(string schemeName, string name, string issuer, params Claim[] claims)
        {
            var identity = new ClaimsIdentity(schemeName);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier,
                              name, ClaimValueTypes.String, issuer));
            identity.AddClaim(new Claim(ClaimTypes.Name, name, ClaimValueTypes.String, issuer));

            identity.AddClaims(claims);

            var principal = new ClaimsPrincipal(identity);
            return principal;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var queryStringHash = ParseApiKey(); // handles parsing QueryString

            if (string.IsNullOrEmpty(queryStringHash)) //no key was provided - return NoResult
                return Task.FromResult(AuthenticateResult.NoResult());

            var match = Options.Devices.FirstOrDefault(d => d.Value == queryStringHash);

            if (Options.Devices.ContainsKey(queryStringHash))
            {
                var deviceName = Options.Devices[queryStringHash];
                var principal = BuildPrincipal(Scheme.Name, deviceName,
                                Options.ClaimsIssuer ?? "QueryString");

                return Task.FromResult(AuthenticateResult.Success
                  (new AuthenticationTicket(principal, Scheme.Name)));
            }

            return Task.FromResult(AuthenticateResult.Fail
                        ($"Invalid QueryString provided."));
        }
    }
}

using System;
using System.IO;
using Microsoft.Extensions.Hosting;

namespace Site.Services.Lightroom
{
    // Persists a refresh token obtained via the in-app OAuth re-auth button, outside
    // of the deployed site content so it survives app restarts/redeploys. On Azure
    // App Service, %HOME% is Azure-Files-backed and persistent across deploys/restarts
    // (unlike the local instance disk); locally, falls back to a gitignored file next
    // to config/secrets.json. This is only ever written by GalleryController's OAuth
    // callback - the static value in secrets.json remains the fallback if no button
    // re-auth has happened yet.
    public class LightroomRefreshTokenStore
    {
        private readonly string _filePath;

        public LightroomRefreshTokenStore(IHostEnvironment env)
        {
            var persistentHome = Environment.GetEnvironmentVariable("HOME");
            var baseDir = !string.IsNullOrEmpty(persistentHome)
                ? Path.Combine(persistentHome, "data")
                : Path.Combine(env.ContentRootPath, "config");

            Directory.CreateDirectory(baseDir);
            _filePath = Path.Combine(baseDir, "lightroom-refresh-token-override.txt");
        }

        public string TryRead() => File.Exists(_filePath) ? File.ReadAllText(_filePath).Trim() : null;

        public void Write(string refreshToken) => File.WriteAllText(_filePath, refreshToken);
    }
}

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EthStatistics.Services
{
    public sealed class UpdateService
    {
        private const string UpdateJsonUrl =
            "https://microsoftfodev.github.io/EthStatistics/downloads/latest-version.json";

        private static readonly HttpClient HttpClient =
            new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

        public async Task CheckAndDownloadUpdateAsync()
        {
            try
            {
                Version currentVersion =
                    GetCurrentVersion();

                string url =
                    $"{UpdateJsonUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                string json =
                    await HttpClient.GetStringAsync(url);

                UpdateInfo? update =
                    JsonSerializer.Deserialize<UpdateInfo>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (update == null)
                {
                    return;
                }

                if (!Version.TryParse(
                        update.Version,
                        out Version? latestVersion))
                {
                    return;
                }

                if (latestVersion <= currentVersion)
                {
                    return;
                }

                if (!IsTrustedUpdateUrl(
                        update.DownloadUrl))
                {
                    return;
                }

                OpenDownloadUrl(
                    update.DownloadUrl);
            }
            catch
            {
                // Never stop EthStatistics from opening
                // because an update check failed.
            }
        }

        public Version GetCurrentVersion()
        {
            Version? version =
                Assembly
                    .GetExecutingAssembly()
                    .GetName()
                    .Version;

            return version ??
                   new Version(
                       0,
                       0,
                       0,
                       0);
        }

        private static bool IsTrustedUpdateUrl(
            string url)
        {
            if (!Uri.TryCreate(
                    url,
                    UriKind.Absolute,
                    out Uri? uri))
            {
                return false;
            }

            return
                uri.Scheme.Equals(
                    Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)
                &&
                uri.Host.Equals(
                    "microsoftfodev.github.io",
                    StringComparison.OrdinalIgnoreCase)
                &&
                uri.AbsolutePath.EndsWith(
                    ".msixbundle",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static void OpenDownloadUrl(
            string url)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
        }


        private sealed class UpdateInfo
        {
            [JsonPropertyName("version")]
            public string Version { get; set; } =
                string.Empty;

            [JsonPropertyName("downloadUrl")]
            public string DownloadUrl { get; set; } =
                string.Empty;

            [JsonPropertyName("releaseNotes")]
            public string ReleaseNotes { get; set; } =
                string.Empty;
        }
    }
}
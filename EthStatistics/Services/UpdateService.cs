using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel;

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

        private static readonly string LogPath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "EthStatistics",
                "update.log"
            );

        public async Task CheckAndDownloadUpdateAsync()
        {
            try
            {
                Version currentVersion =
                    GetCurrentVersion();

                string requestUrl =
                    $"{UpdateJsonUrl}?nocache={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                string json =
                    await HttpClient.GetStringAsync(
                        requestUrl);

                UpdateInfo? update =
                    JsonSerializer.Deserialize<UpdateInfo>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (update == null)
                {
                    WriteLog(
                        $"Current={currentVersion}; JSON could not be parsed.");

                    return;
                }

                if (!Version.TryParse(
                        update.Version,
                        out Version? latestVersion))
                {
                    WriteLog(
                        $"Current={currentVersion}; Invalid public version={update.Version}");

                    return;
                }

                WriteLog(
                    $"Current={currentVersion}; Latest={latestVersion}");

                // This is the critical check.
                // Browser opens ONLY if website version is newer.
                if (latestVersion <= currentVersion)
                {
                    WriteLog(
                        "No update required.");

                    return;
                }

                if (!IsTrustedUpdateUrl(
                        update.DownloadUrl))
                {
                    WriteLog(
                        $"Rejected update URL: {update.DownloadUrl}");

                    return;
                }

                WriteLog(
                    $"Update available. Opening {update.DownloadUrl}");

                OpenDownloadUrl(
                    update.DownloadUrl);
            }
            catch (Exception ex)
            {
                WriteLog(
                    $"Update check failed: {ex}");
            }
        }

        private static Version GetCurrentVersion()
        {
            PackageVersion packageVersion =
                Package.Current.Id.Version;

            return new Version(
                packageVersion.Major,
                packageVersion.Minor,
                packageVersion.Build,
                packageVersion.Revision);
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

        private static void WriteLog(
            string message)
        {
            try
            {
                string? directory =
                    Path.GetDirectoryName(
                        LogPath);

                if (!string.IsNullOrWhiteSpace(
                        directory))
                {
                    Directory.CreateDirectory(
                        directory);
                }

                File.AppendAllText(
                    LogPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}{Environment.NewLine}");
            }
            catch
            {
                // Logging must never prevent startup.
            }
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
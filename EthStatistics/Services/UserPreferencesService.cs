using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EthStatistics.Settings;

namespace EthStatistics.Services
{
    public sealed class UserPreferencesService
    {
        private readonly string _settingsDirectory;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true
            };


        public UserPreferencesService()
        {
            string localApplicationData =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData
                );

            _settingsDirectory =
                Path.Combine(
                    localApplicationData,
                    "EthStatistics",
                    "Preferences"
                );
        }


        public async Task<UserPreferences> LoadAsync(
            string? accountId = null)
        {
            string settingsFile =
                GetSettingsFile(
                    accountId
                );

            if (!File.Exists(settingsFile))
            {
                return new UserPreferences();
            }

            try
            {
                string json =
                    await File.ReadAllTextAsync(
                        settingsFile
                    );

                UserPreferences? preferences =
                    JsonSerializer.Deserialize<UserPreferences>(
                        json
                    );

                return preferences ??
                    new UserPreferences();
            }
            catch
            {
                return new UserPreferences();
            }
        }


        public async Task SaveAsync(
            UserPreferences preferences,
            string? accountId = null)
        {
            Directory.CreateDirectory(
                _settingsDirectory
            );

            string settingsFile =
                GetSettingsFile(
                    accountId
                );

            string json =
                JsonSerializer.Serialize(
                    preferences,
                    JsonOptions
                );

            await File.WriteAllTextAsync(
                settingsFile,
                json
            );
        }


        private string GetSettingsFile(
            string? accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return Path.Combine(
                    _settingsDirectory,
                    "guest.json"
                );
            }

            string accountKey =
                CreateAccountKey(
                    accountId
                );

            return Path.Combine(
                _settingsDirectory,
                $"user-{accountKey}.json"
            );
        }


        private static string CreateAccountKey(
            string accountId)
        {
            byte[] bytes =
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        accountId
                    )
                );

            return Convert
                .ToHexString(bytes)
                .ToLowerInvariant();
        }


        public string GetSettingsFilePath(
            string? accountId = null)
        {
            return GetSettingsFile(
                accountId
            );
        }
    }
}
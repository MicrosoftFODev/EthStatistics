using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EthStatistics.Settings;

namespace EthStatistics.Services
{
    public sealed class UserPreferencesService
    {
        private readonly string _settingsDirectory;
        private readonly string _settingsFile;


        private static readonly JsonSerializerOptions
            JsonOptions =
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
                    "CryptoStatistics"
                );


            _settingsFile =
                Path.Combine(
                    _settingsDirectory,
                    "preferences.json"
                );
        }


        public async Task<UserPreferences> LoadAsync()
        {
            if (!File.Exists(_settingsFile))
            {
                return new UserPreferences();
            }


            try
            {
                string json =
                    await File.ReadAllTextAsync(
                        _settingsFile
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
            UserPreferences preferences)
        {
            Directory.CreateDirectory(
                _settingsDirectory
            );


            string json =
                JsonSerializer.Serialize(
                    preferences,
                    JsonOptions
                );


            await File.WriteAllTextAsync(
                _settingsFile,
                json
            );
        }


        public string SettingsFilePath =>
            _settingsFile;
    }
}
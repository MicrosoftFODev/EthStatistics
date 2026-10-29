using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EthStatistics.Services;
using EthStatistics.Settings;

namespace EthStatistics.Views
{
    public partial class SettingsView : UserControl
    {
        // ==========================================
        // SERVICES / STATE
        // ==========================================

        private readonly UserPreferencesService
            _preferencesService;

        private readonly UserPreferences
            _preferences;

        private readonly string?
            _accountId;

        private bool
            _initialized;


        // ==========================================
        // EVENTS
        // ==========================================

        public event EventHandler<DataPreferencesChangedEventArgs>?
            DataPreferencesChanged;

        public event EventHandler<GeneralPreferencesChangedEventArgs>?
            GeneralPreferencesChanged;


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public SettingsView(
            UserPreferences preferences,
            UserPreferencesService preferencesService,
            string? accountId)
        {
            InitializeComponent();

            _preferences =
                preferences;

            _preferencesService =
                preferencesService;

            _accountId =
                accountId;


            Loaded +=
                SettingsView_Loaded;

            Unloaded +=
                SettingsView_Unloaded;
        }


        // ==========================================
        // LOAD
        // ==========================================

        private void SettingsView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            InitializeControls();
        }


        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void InitializeControls()
        {
            _initialized =
                false;


            RemoveControlEvents();


            PopulateLanguageOptions();

            PopulateThemeOptions();

            PopulateMarketOptions();


            AddControlEvents();


            _initialized =
                true;
        }


        // ==========================================
        // LANGUAGE
        // ==========================================

        private void PopulateLanguageOptions()
        {
            LanguageComboBox.ItemsSource =
                new[]
                {
                    "English",
                    "Greek"
                };


            if (
                !string.IsNullOrWhiteSpace(
                    _preferences.Language
                )
                &&
                LanguageComboBox.Items.Contains(
                    _preferences.Language
                )
            )
            {
                LanguageComboBox.SelectedItem =
                    _preferences.Language;
            }
            else if (
                LanguageComboBox.Items.Count > 0
            )
            {
                LanguageComboBox.SelectedIndex =
                    0;
            }
        }


        // ==========================================
        // THEME
        // ==========================================

        private void PopulateThemeOptions()
        {
            ThemeComboBox.ItemsSource =
                new[]
                {
                    "Dark",
                    "Light",
                    "System"
                };


            if (
                !string.IsNullOrWhiteSpace(
                    _preferences.Theme
                )
                &&
                ThemeComboBox.Items.Contains(
                    _preferences.Theme
                )
            )
            {
                ThemeComboBox.SelectedItem =
                    _preferences.Theme;
            }
            else if (
                ThemeComboBox.Items.Count > 0
            )
            {
                ThemeComboBox.SelectedIndex =
                    0;
            }
        }


        // ==========================================
        // MARKET OPTIONS
        // ==========================================

        private void PopulateMarketOptions()
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            CurrencyComboBox.ItemsSource =
                store.QuoteCurrencies;


            if (
                !string.IsNullOrWhiteSpace(
                    _preferences.QuoteCurrency
                )
                &&
                store.QuoteCurrencies.Contains(
                    _preferences.QuoteCurrency
                )
            )
            {
                CurrencyComboBox.SelectedItem =
                    _preferences.QuoteCurrency;
            }
            else if (
                CurrencyComboBox.Items.Count > 0
            )
            {
                CurrencyComboBox.SelectedIndex =
                    0;
            }
        }


        // ==========================================
        // EVENTS
        // ==========================================

        private void AddControlEvents()
        {
            LanguageComboBox.SelectionChanged +=
                GeneralPreference_SelectionChanged;


            ThemeComboBox.SelectionChanged +=
                GeneralPreference_SelectionChanged;


            CurrencyComboBox.SelectionChanged +=
                DataPreference_SelectionChanged;
        }


        private void RemoveControlEvents()
        {
            LanguageComboBox.SelectionChanged -=
                GeneralPreference_SelectionChanged;


            ThemeComboBox.SelectionChanged -=
                GeneralPreference_SelectionChanged;


            CurrencyComboBox.SelectionChanged -=
                DataPreference_SelectionChanged;
        }


        // ==========================================
        // GENERAL PREFERENCES
        // ==========================================

        private async void GeneralPreference_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }


            UpdatePreferencesFromControls();


            await SavePreferencesAsync();


            GeneralPreferencesChanged?.Invoke(
                this,
                new GeneralPreferencesChangedEventArgs
                {
                    Language =
                        _preferences.Language,

                    Theme =
                        _preferences.Theme
                }
            );
        }


        // ==========================================
        // DATA PREFERENCES
        // ==========================================

        private async void DataPreference_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }


            UpdatePreferencesFromControls();


            await SavePreferencesAsync();


            RaiseDataPreferencesChanged();
        }


        // ==========================================
        // UPDATE PREFERENCES
        // ==========================================

        private void UpdatePreferencesFromControls()
        {
            _preferences.Language =
                LanguageComboBox
                    .SelectedItem?
                    .ToString();


            _preferences.Theme =
                ThemeComboBox
                    .SelectedItem?
                    .ToString();


            _preferences.QuoteCurrency =
                CurrencyComboBox
                    .SelectedItem?
                    .ToString();
        }


        // ==========================================
        // SAVE PROFILE
        // ==========================================

        private async Task SavePreferencesAsync()
        {
            await _preferencesService.SaveAsync(
                _preferences,
                _accountId
            );
        }


        // ==========================================
        // DATA EVENT
        // ==========================================

        private void RaiseDataPreferencesChanged()
        {
            if (
                string.IsNullOrWhiteSpace(
                    _preferences.QuoteCurrency
                )
            )
            {
                return;
            }


            DataPreferencesChanged?.Invoke(
                this,
                new DataPreferencesChangedEventArgs
                {
                    QuoteCurrency =
                        _preferences.QuoteCurrency
                }
            );
        }


        // ==========================================
        // SAVE BUTTON
        // ==========================================

        private async void SavePreferences_Click(
            object sender,
            RoutedEventArgs e)
        {
            UpdatePreferencesFromControls();


            await SavePreferencesAsync();


            string profileDescription =
                string.IsNullOrWhiteSpace(
                    _accountId
                )
                    ? "Guest preferences saved."
                    : "Your preferences have been saved.";


            MessageBox.Show(
                profileDescription,
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }


        // ==========================================
        // UNLOAD
        // ==========================================

        private void SettingsView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            RemoveControlEvents();
        }
    }
}
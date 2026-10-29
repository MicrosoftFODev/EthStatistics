using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EthStatistics.Models;
using EthStatistics.Services;
using EthStatistics.Settings;

namespace EthStatistics.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly UserPreferencesService
            _preferencesService;

        private readonly UserPreferences
            _preferences;

        private bool _initialized;


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
            UserPreferencesService preferencesService)
        {
            InitializeComponent();

            _preferences =
                preferences;

            _preferencesService =
                preferencesService;


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
            _initialized = false;


            RemoveControlEvents();


            PopulateLanguageOptions();

            PopulateThemeOptions();

            PopulateMarketOptions();


            AddControlEvents();


            _initialized = true;
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


            // Quote currencies come from provider
            // metadata.

            CurrencyComboBox.ItemsSource =
                store.QuoteCurrencies;


            // Intervals come from provider metadata.

            TimeframeComboBox.ItemsSource =
                store.Intervals;


            TimeframeComboBox.DisplayMemberPath =
                nameof(MarketInterval.Name);


            TimeframeComboBox.SelectedValuePath =
                nameof(MarketInterval.Minutes);


            // ======================================
            // RESTORE QUOTE CURRENCY
            // ======================================

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


            // ======================================
            // RESTORE INTERVAL
            // ======================================

            MarketInterval? selectedInterval =
                null;


            if (
                _preferences.IntervalMinutes.HasValue
            )
            {
                selectedInterval =
                    store.Intervals.FirstOrDefault(
                        interval =>
                            interval.Minutes ==
                            _preferences.IntervalMinutes.Value
                    );
            }


            selectedInterval ??=
                store.Intervals.FirstOrDefault(
                    interval =>
                        interval.IsDefault
                )
                ?? store.Intervals.FirstOrDefault();


            if (selectedInterval is not null)
            {
                TimeframeComboBox.SelectedValue =
                    selectedInterval.Minutes;
            }
        }


        // ==========================================
        // EVENT MANAGEMENT
        // ==========================================

        private void AddControlEvents()
        {
            LanguageComboBox.SelectionChanged +=
                GeneralPreference_SelectionChanged;


            ThemeComboBox.SelectionChanged +=
                GeneralPreference_SelectionChanged;


            CurrencyComboBox.SelectionChanged +=
                DataPreference_SelectionChanged;


            TimeframeComboBox.SelectionChanged +=
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


            TimeframeComboBox.SelectionChanged -=
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


            await _preferencesService.SaveAsync(
                _preferences
            );


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


            await _preferencesService.SaveAsync(
                _preferences
            );


            RaiseDataPreferencesChanged();
        }


        // ==========================================
        // UPDATE MODEL
        // ==========================================

        private void UpdatePreferencesFromControls()
        {
            _preferences.Language =
                LanguageComboBox.SelectedItem?
                    .ToString();


            _preferences.Theme =
                ThemeComboBox.SelectedItem?
                    .ToString();


            _preferences.QuoteCurrency =
                CurrencyComboBox.SelectedItem?
                    .ToString();


            if (
                TimeframeComboBox.SelectedValue
                is int interval
            )
            {
                _preferences.IntervalMinutes =
                    interval;
            }
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
                ||
                !_preferences.IntervalMinutes.HasValue
            )
            {
                return;
            }


            DataPreferencesChanged?.Invoke(
                this,
                new DataPreferencesChangedEventArgs
                {
                    QuoteCurrency =
                        _preferences.QuoteCurrency,

                    Interval =
                        _preferences.IntervalMinutes.Value
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


            await _preferencesService.SaveAsync(
                _preferences
            );


            MessageBox.Show(
                "Preferences saved.",
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
using System;
using System.Windows;
using System.Windows.Controls;

namespace EthStatistics.Views
{
    public partial class SettingsView : UserControl
    {
        private bool _initialized;

        public event EventHandler<DataPreferencesChangedEventArgs>?
            DataPreferencesChanged;

        public SettingsView()
        {
            InitializeComponent();

            // Default UI selections
            LanguageComboBox.SelectedIndex = 0;
            ThemeComboBox.SelectedIndex = 0;
            CurrencyComboBox.SelectedIndex = 0;
            TimeframeComboBox.SelectedIndex = 1;

            // These settings affect the requested market data,
            // so changing them triggers a refresh.
            CurrencyComboBox.SelectionChanged +=
                DataOption_SelectionChanged;

            TimeframeComboBox.SelectionChanged +=
                DataOption_SelectionChanged;

            _initialized = true;
        }


        private void DataOption_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }

            RaiseDataPreferencesChanged();
        }


        private void RaiseDataPreferencesChanged()
        {
            string currency =
                GetSelectedText(CurrencyComboBox);

            string timeframe =
                GetSelectedText(TimeframeComboBox);

            int interval = timeframe switch
            {
                "Hourly" => 60,
                "Daily" => 1440,
                "Weekly" => 10080,
                _ => 1440
            };

            DataPreferencesChanged?.Invoke(
                this,
                new DataPreferencesChangedEventArgs
                {
                    Currency = currency,
                    Interval = interval
                }
            );
        }


        private static string GetSelectedText(
            ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString() ?? "";
            }

            return "";
        }


        private void SavePreferences_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Preferences saved for this session.",
                "Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
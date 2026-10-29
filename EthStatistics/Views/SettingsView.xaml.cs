using System.Windows;
using System.Windows.Controls;

namespace EthStatistics.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            LanguageComboBox.SelectedIndex = 0;
            ThemeComboBox.SelectedIndex = 0;
            CurrencyComboBox.SelectedIndex = 0;
            TimeframeComboBox.SelectedIndex = 1;
        }

        private void SavePreferences_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Preferences saved for this session.",
                "Settings"
            );
        }
    }
}
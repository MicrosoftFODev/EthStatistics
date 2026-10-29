using System.Windows;
using EthStatistics.Views;

namespace EthStatistics
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ShowDashboard();
        }

        private void ShowDashboard()
        {
            MainContent.Content = new DashboardView();
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void ChartButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new ChartView();
        }

        private void StatisticsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new StatisticsView();
        }

        private void ForecastButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new ForecastView();
        }

        private void SettingsMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content = new SettingsView();
        }

        private void AboutMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Crypto Statistics\nQuantitative cryptocurrency analysis.",
                "About"
            );
        }
    }
}
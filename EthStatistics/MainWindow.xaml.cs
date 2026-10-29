using System.Windows;
using System.Windows.Media;
using EthStatistics.Models;
using EthStatistics.Services;
using EthStatistics.Views;

namespace EthStatistics
{
    public partial class MainWindow : Window
    {
        private readonly PythonMarketDataService
            _marketDataService = new();

        private CancellationTokenSource?
            _refreshCancellation;

        public MainWindow()
        {
            InitializeComponent();

            ShowDashboard();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await LoadMetadataAsync();
        }

        private async Task LoadMetadataAsync()
        {
            ShowLoadingStatus(
                "Loading available markets..."
            );

            try
            {
                MarketMetadataResult result =
                    await _marketDataService.GetMetadataAsync();

                MarketDataStore.Instance.SetMetadata(result);

                if (!result.Success)
                {
                    ShowErrorStatus(
                        result.Error ??
                        "Market metadata could not be loaded."
                    );

                    return;
                }

                KrakenMarket? firstMarket =
                    result.Markets.FirstOrDefault();

                MarketInterval? defaultInterval =
                    result.Intervals.FirstOrDefault(
                        interval => interval.IsDefault
                    )
                    ?? result.Intervals.FirstOrDefault();

                MarketPeriod? defaultPeriod =
                    result.Periods.FirstOrDefault(
                        period => period.IsDefault
                    )
                    ?? result.Periods.FirstOrDefault();

                if (
                    firstMarket is null ||
                    defaultInterval is null ||
                    defaultPeriod is null
                )
                {
                    ShowErrorStatus(
                        "Kraken returned no usable market metadata."
                    );

                    return;
                }

                await ApplySelectionAndRefreshAsync(
                    firstMarket.Pair,
                    defaultInterval.Minutes,
                    defaultPeriod.Days
                );
            }
            catch (Exception error)
            {
                MarketDataStore.Instance.SetError(
                    error.Message
                );

                ShowErrorStatus(error.Message);
            }
        }

        private async Task ApplySelectionAndRefreshAsync(
            string pair,
            int interval,
            int periodDays)
        {
            MarketDataStore.Instance.SetSelection(
                pair,
                interval,
                periodDays
            );

            await RefreshMarketDataAsync();
        }

        private async Task RefreshMarketDataAsync()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            if (
                string.IsNullOrWhiteSpace(store.Pair) ||
                store.Interval is null
            )
            {
                return;
            }

            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();

            _refreshCancellation =
                new CancellationTokenSource();

            string pair = store.Pair;
            int interval = store.Interval.Value;

            MarketDataStore.Instance.SetLoading();
            ShowLoadingStatus($"Loading {pair}...");

            try
            {
                MarketDataResult result =
                    await _marketDataService.GetMarketDataAsync(
                        pair,
                        interval,
                        _refreshCancellation.Token
                    );

                MarketDataStore.Instance.SetResult(result);

                if (result.Success)
                {
                    ShowSuccessStatus(
                        result.Pair,
                        result.Candles.Count
                    );
                }
                else
                {
                    ShowErrorStatus(
                        result.Error ??
                        "Market data could not be loaded."
                    );
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception error)
            {
                MarketDataStore.Instance.SetError(
                    error.Message
                );

                ShowErrorStatus(error.Message);
            }
        }

        private void ChartButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var chartView = new ChartView();

            chartView.ChartPreferencesChanged +=
                ChartPreferencesChanged;

            MainContent.Content = chartView;
        }

        private async void ChartPreferencesChanged(
            object? sender,
            ChartPreferencesChangedEventArgs e)
        {
            await ApplySelectionAndRefreshAsync(
                e.Pair,
                e.Interval,
                e.PeriodDays
            );
        }

        private void ShowLoadingStatus(string message)
        {
            DataStatusText.Text = message;

            DataStatusIndicator.Fill =
                FindBrush("WarningBrush");
        }

        private void ShowSuccessStatus(
            string pair,
            int count)
        {
            DataStatusText.Text =
                $"{pair} · {count} observations";

            DataStatusIndicator.Fill =
                FindBrush("PositiveBrush");
        }

        private void ShowErrorStatus(string error)
        {
            DataStatusText.Text =
                "Market data unavailable";

            DataStatusIndicator.Fill =
                FindBrush("NegativeBrush");

            MessageBox.Show(
                error,
                "Market Data Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private Brush FindBrush(string resourceName)
        {
            return (Brush)FindResource(resourceName);
        }

        private void ShowDashboard()
        {
            MainContent.Content =
                new DashboardView();
        }

        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void StatisticsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content =
                new StatisticsView();
        }

        private void ForecastButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content =
                new ForecastView();
        }

        private void SettingsMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content =
                new SettingsView();
        }

        private void AboutMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Crypto Statistics\n" +
                "Quantitative cryptocurrency analysis.",
                "About"
            );
        }
    }
}
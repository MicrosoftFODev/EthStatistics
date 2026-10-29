using System.Windows.Controls;
using System.Windows.Media;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class ChartView : UserControl
    {
        private bool _initialized;

        public event EventHandler<ChartPreferencesChangedEventArgs>?
            ChartPreferencesChanged;

        public ChartView()
        {
            InitializeComponent();

            Loaded += ChartView_Loaded;
            Unloaded += ChartView_Unloaded;

            MarketComboBox.SelectionChanged += SelectionChanged;
            IntervalComboBox.SelectionChanged += SelectionChanged;
            PeriodComboBox.SelectionChanged += SelectionChanged;

            MarketDataStore.Instance.Changed += StoreChanged;
        }

        private void ChartView_Loaded(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            PopulateFromStore();
            UpdateStatus();
        }

        private void PopulateFromStore()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            _initialized = false;

            MarketComboBox.ItemsSource = store.Markets;
            IntervalComboBox.ItemsSource = store.Intervals;
            PeriodComboBox.ItemsSource = store.Periods;

            if (
                store.Pair is not null &&
                store.Markets.Any(
                    market => market.Pair == store.Pair
                )
            )
            {
                MarketComboBox.SelectedValue =
                    store.Pair;
            }
            else
            {
                MarketComboBox.SelectedIndex =
                    store.Markets.Count > 0 ? 0 : -1;
            }

            MarketInterval? selectedInterval =
                store.Intervals.FirstOrDefault(
                    interval =>
                        interval.Minutes == store.Interval
                )
                ?? store.Intervals.FirstOrDefault(
                    interval => interval.IsDefault
                )
                ?? store.Intervals.FirstOrDefault();

            if (selectedInterval is not null)
            {
                IntervalComboBox.SelectedValue =
                    selectedInterval.Minutes;
            }

            MarketPeriod? selectedPeriod =
                store.Periods.FirstOrDefault(
                    period =>
                        period.Days == store.PeriodDays
                )
                ?? store.Periods.FirstOrDefault(
                    period => period.IsDefault
                )
                ?? store.Periods.FirstOrDefault();

            if (selectedPeriod is not null)
            {
                PeriodComboBox.SelectedValue =
                    selectedPeriod.Days;
            }

            _initialized = true;

            RaiseSelectionChanged();
        }

        private void SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_initialized)
            {
                return;
            }

            RaiseSelectionChanged();
        }

        private void RaiseSelectionChanged()
        {
            if (
                MarketComboBox.SelectedValue is not string pair ||
                IntervalComboBox.SelectedValue is not int interval ||
                PeriodComboBox.SelectedValue is not int periodDays
            )
            {
                return;
            }

            ChartPreferencesChanged?.Invoke(
                this,
                new ChartPreferencesChangedEventArgs
                {
                    Pair = pair,
                    Interval = interval,
                    PeriodDays = periodDays
                }
            );
        }

        private void StoreChanged(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Invoke(UpdateStatus);
        }

        private void UpdateStatus()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            if (store.IsLoading)
            {
                ChartMarketText.Text =
                    $"Loading {store.Pair}...";

                RefreshStatusText.Text =
                    "Refreshing";

                RefreshStatusIndicator.Fill =
                    FindBrush("WarningBrush");

                return;
            }

            if (!string.IsNullOrWhiteSpace(store.Error))
            {
                ChartMarketText.Text =
                    $"{store.Pair} · Data unavailable";

                RefreshStatusText.Text =
                    "Unavailable";

                RefreshStatusIndicator.Fill =
                    FindBrush("NegativeBrush");

                return;
            }

            if (store.Candles.Count > 0)
            {
                ChartMarketText.Text =
                    $"{store.Pair} · " +
                    $"{store.Candles.Count} observations";

                RefreshStatusText.Text =
                    "Updated";

                RefreshStatusIndicator.Fill =
                    FindBrush("PositiveBrush");

                VolumeMarketText.Text =
                    store.Pair ?? "—";

                return;
            }

            ChartMarketText.Text =
                "Waiting for market data";

            RefreshStatusText.Text =
                "Waiting";

            RefreshStatusIndicator.Fill =
                FindBrush("TextMutedBrush");
        }

        private Brush FindBrush(string resourceName)
        {
            return (Brush)FindResource(resourceName);
        }

        private void ChartView_Unloaded(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                StoreChanged;
        }
    }
}
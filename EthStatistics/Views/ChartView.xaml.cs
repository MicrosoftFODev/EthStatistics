using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class ChartView : UserControl
    {
        private bool _initialized;
        private bool _isImportedDataset;

        public event EventHandler<ChartPreferencesChangedEventArgs>?
            ChartPreferencesChanged;


        public ChartView()
        {
            InitializeComponent();

            Loaded += ChartView_Loaded;
            Unloaded += ChartView_Unloaded;

            MarketComboBox.SelectionChanged +=
                SelectionChanged;

            IntervalComboBox.SelectionChanged +=
                SelectionChanged;

            PeriodComboBox.SelectionChanged +=
                SelectionChanged;

            MarketDataStore.Instance.Changed +=
                StoreChanged;
        }


        private void ChartView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateImportState();

            PopulateFromStore();

            UpdateFromStore();
        }


        private void UpdateImportState()
        {
            string dataSource =
                MarketDataStore.Instance.DataSource ?? "";

            _isImportedDataset =
                dataSource.StartsWith(
                    "Excel",
                    StringComparison.OrdinalIgnoreCase
                );
        }


        private void PopulateFromStore()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            _initialized = false;


            MarketComboBox.ItemsSource =
                store.Markets;

            IntervalComboBox.ItemsSource =
                store.Intervals;

            PeriodComboBox.ItemsSource =
                store.Periods;


            if (_isImportedDataset)
            {
                MarketComboBox.SelectedIndex = -1;
                IntervalComboBox.SelectedIndex = -1;
                PeriodComboBox.SelectedIndex = -1;

                MarketComboBox.IsEnabled = false;
                IntervalComboBox.IsEnabled = false;
                PeriodComboBox.IsEnabled = false;

                _initialized = true;

                return;
            }


            MarketComboBox.IsEnabled = true;
            IntervalComboBox.IsEnabled = true;
            PeriodComboBox.IsEnabled = true;


            if (
                store.Pair is not null &&
                store.Markets.Any(
                    market =>
                        market.Pair == store.Pair
                )
            )
            {
                MarketComboBox.SelectedValue =
                    store.Pair;
            }
            else if (store.Markets.Count > 0)
            {
                MarketComboBox.SelectedIndex = 0;
            }


            MarketInterval? selectedInterval =
                store.Intervals.FirstOrDefault(
                    item =>
                        item.Minutes ==
                        store.Interval
                )
                ??
                store.Intervals.FirstOrDefault(
                    item =>
                        item.IsDefault
                )
                ??
                store.Intervals.FirstOrDefault();


            if (selectedInterval is not null)
            {
                IntervalComboBox.SelectedValue =
                    selectedInterval.Minutes;
            }


            MarketPeriod? selectedPeriod =
                store.Periods.FirstOrDefault(
                    item =>
                        item.Days ==
                        store.PeriodDays
                )
                ??
                store.Periods.FirstOrDefault(
                    item =>
                        item.IsDefault
                )
                ??
                store.Periods.FirstOrDefault();


            if (selectedPeriod is not null)
            {
                PeriodComboBox.SelectedValue =
                    selectedPeriod.Days;
            }


            _initialized = true;
        }


        private void SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (
                !_initialized ||
                _isImportedDataset
            )
            {
                return;
            }

            RaiseSelectionChanged();
        }


        private void RaiseSelectionChanged()
        {
            if (
                MarketComboBox.SelectedValue
                    is not string pair
                ||
                IntervalComboBox.SelectedValue
                    is not int interval
                ||
                PeriodComboBox.SelectedValue
                    is not int periodDays
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
            Dispatcher.Invoke(
                () =>
                {
                    UpdateImportState();
                    UpdateFromStore();
                }
            );
        }


        private void UpdateFromStore()
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

                ClearChart();

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

                ClearChart();

                return;
            }


            if (store.Candles.Count == 0)
            {
                ChartMarketText.Text =
                    "Waiting for market data";

                RefreshStatusText.Text =
                    "Waiting";

                RefreshStatusIndicator.Fill =
                    FindBrush("TextMutedBrush");

                VolumeMarketText.Text =
                    "—";

                ClearChart();

                return;
            }


            ChartMarketText.Text =
                $"{store.Pair} · " +
                $"{store.Candles.Count} observations";


            RefreshStatusText.Text =
                _isImportedDataset
                    ? "Imported"
                    : "Updated";


            RefreshStatusIndicator.Fill =
                FindBrush("PositiveBrush");


            VolumeMarketText.Text =
                store.DataSource;


            DrawPriceChart(
                store.Candles
            );


            DrawVolumeChart(
                store.Candles
            );
        }


        private void DrawPriceChart(
            IReadOnlyList<MarketCandle> candles)
        {
            double width =
                PriceCanvas.ActualWidth;

            double height =
                PriceCanvas.ActualHeight;


            if (
                width <= 1 ||
                height <= 1 ||
                candles.Count < 2
            )
            {
                return;
            }


            double[] values =
                candles
                    .Select(
                        candle =>
                            candle.Close
                    )
                    .Where(
                        double.IsFinite
                    )
                    .ToArray();


            if (values.Length < 2)
            {
                ClearChart();

                return;
            }


            double minimum =
                values.Min();

            double maximum =
                values.Max();

            double range =
                maximum - minimum;


            if (range <= 0)
            {
                range = 1;
            }


            double verticalPadding =
                6.0;


            double drawableHeight =
                Math.Max(
                    1,
                    height -
                    verticalPadding * 2
                );


            var points =
                new PointCollection(
                    values.Length
                );


            for (
                int index = 0;
                index < values.Length;
                index++)
            {
                double x =
                    index *
                    width /
                    (values.Length - 1);


                double normalized =
                    (
                        values[index] -
                        minimum
                    )
                    /
                    range;


                double y =
                    verticalPadding +
                    (
                        1.0 -
                        normalized
                    )
                    *
                    drawableHeight;


                points.Add(
                    new Point(
                        x,
                        y
                    )
                );
            }


            PricePolyline.Points =
                points;


            MaximumChartValueText.Text =
                maximum.ToString("N2");


            MinimumChartValueText.Text =
                minimum.ToString("N2");


            EmptyChartState.Visibility =
                Visibility.Collapsed;


            PricePolyline.Visibility =
                Visibility.Visible;
        }


        private void DrawVolumeChart(
            IReadOnlyList<MarketCandle> candles)
        {
            VolumeCanvas.Children.Clear();


            double width =
                VolumeCanvas.ActualWidth;

            double height =
                VolumeCanvas.ActualHeight;


            if (
                width <= 1 ||
                height <= 1
            )
            {
                return;
            }


            double maximumVolume =
                candles
                    .Select(
                        candle =>
                            candle.Volume
                    )
                    .Where(
                        double.IsFinite
                    )
                    .DefaultIfEmpty(0)
                    .Max();


            if (maximumVolume <= 0)
            {
                EmptyVolumeState.Visibility =
                    Visibility.Visible;

                return;
            }


            double barWidth =
                Math.Max(
                    1,
                    width /
                    candles.Count
                );


            for (
                int index = 0;
                index < candles.Count;
                index++)
            {
                double volume =
                    Math.Max(
                        0,
                        candles[index].Volume
                    );


                double barHeight =
                    volume /
                    maximumVolume *
                    height;


                var rectangle =
                    new Rectangle
                    {
                        Width =
                            Math.Max(
                                1,
                                barWidth - 1
                            ),

                        Height =
                            barHeight,

                        Fill =
                            FindBrush(
                                "AccentSoftBrush"
                            )
                    };


                Canvas.SetLeft(
                    rectangle,
                    index * barWidth
                );


                Canvas.SetTop(
                    rectangle,
                    height - barHeight
                );


                VolumeCanvas.Children.Add(
                    rectangle
                );
            }


            EmptyVolumeState.Visibility =
                Visibility.Collapsed;
        }


        private void ClearChart()
        {
            PricePolyline.Points =
                new PointCollection();


            PricePolyline.Visibility =
                Visibility.Collapsed;


            MaximumChartValueText.Text =
                "";

            MinimumChartValueText.Text =
                "";


            EmptyChartState.Visibility =
                Visibility.Visible;


            VolumeCanvas.Children.Clear();


            EmptyVolumeState.Visibility =
                Visibility.Visible;
        }


        private void ChartContainer_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            if (store.Candles.Count == 0)
            {
                return;
            }


            DrawPriceChart(
                store.Candles
            );


            DrawVolumeChart(
                store.Candles
            );
        }


        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }


        private void ChartView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                StoreChanged;


            MarketComboBox.SelectionChanged -=
                SelectionChanged;


            IntervalComboBox.SelectionChanged -=
                SelectionChanged;


            PeriodComboBox.SelectionChanged -=
                SelectionChanged;
        }
    }
}
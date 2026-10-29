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
        // ==========================================
        // STATE
        // ==========================================

        private const int DailyInterval =
            1440;

        private bool
            _initialized;

        private bool
            _isImportedDataset;


        // ==========================================
        // EVENTS
        // ==========================================

        public event EventHandler<ChartPreferencesChangedEventArgs>?
            ChartPreferencesChanged;


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public ChartView()
        {
            InitializeComponent();

            Loaded +=
                ChartView_Loaded;

            Unloaded +=
                ChartView_Unloaded;

            MarketComboBox.SelectionChanged +=
                SelectionChanged;

            PeriodComboBox.SelectionChanged +=
                SelectionChanged;

            MarketDataStore.Instance.Changed +=
                StoreChanged;
        }


        // ==========================================
        // LOAD
        // ==========================================

        private void ChartView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateImportState();

            PopulateFromStore();

            UpdateFromStore();
        }


        // ==========================================
        // IMPORT STATE
        // ==========================================

        private void UpdateImportState()
        {
            string dataSource =
                MarketDataStore.Instance.DataSource
                ?? "";

            _isImportedDataset =
                dataSource.StartsWith(
                    "Excel",
                    StringComparison.OrdinalIgnoreCase
                );
        }


        // ==========================================
        // POPULATE CONTROLS
        // ==========================================

        private void PopulateFromStore()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            _initialized =
                false;

            MarketComboBox.ItemsSource =
                store.Markets;

            PeriodComboBox.ItemsSource =
                store.Periods;


            // --------------------------------------
            // EXCEL DATA
            // --------------------------------------

            if (_isImportedDataset)
            {
                MarketComboBox.SelectedIndex =
                    -1;

                PeriodComboBox.SelectedIndex =
                    -1;

                MarketComboBox.IsEnabled =
                    false;

                PeriodComboBox.IsEnabled =
                    false;

                _initialized =
                    true;

                return;
            }


            // --------------------------------------
            // API DATA
            // --------------------------------------

            MarketComboBox.IsEnabled =
                true;

            PeriodComboBox.IsEnabled =
                true;


            // --------------------------------------
            // RESTORE MARKET
            // --------------------------------------

            KrakenMarket? selectedMarket =
                null;

            if (
                !string.IsNullOrWhiteSpace(
                    store.Pair
                )
            )
            {
                selectedMarket =
                    store.Markets.FirstOrDefault(
                        market =>
                            string.Equals(
                                market.Pair,
                                store.Pair,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );
            }

            selectedMarket ??=
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.Pair,
                            "ETHUSD",
                            StringComparison.OrdinalIgnoreCase
                        )
                );

            selectedMarket ??=
                store.Markets.FirstOrDefault();

            if (selectedMarket is not null)
            {
                MarketComboBox.SelectedValue =
                    selectedMarket.Pair;
            }


            // --------------------------------------
            // RESTORE PERIOD
            // --------------------------------------

            MarketPeriod? selectedPeriod =
                store.Periods.FirstOrDefault(
                    period =>
                        period.Days ==
                        store.PeriodDays
                );

            selectedPeriod ??=
                store.Periods.FirstOrDefault(
                    period =>
                        period.IsDefault
                );

            selectedPeriod ??=
                store.Periods.FirstOrDefault();

            if (selectedPeriod is not null)
            {
                PeriodComboBox.SelectedValue =
                    selectedPeriod.Days;
            }

            _initialized =
                true;
        }


        // ==========================================
        // USER SELECTION
        // ==========================================

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
                    Pair =
                        pair,

                    Interval =
                        DailyInterval,

                    PeriodDays =
                        periodDays
                }
            );
        }


        // ==========================================
        // STORE CHANGED
        // ==========================================

        private void StoreChanged(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Invoke(
                () =>
                {
                    UpdateImportState();

                    UpdateControlState();

                    UpdateFromStore();
                }
            );
        }


        private void UpdateControlState()
        {
            ApiControlsVisibility();

            if (_isImportedDataset)
            {
                MarketComboBox.IsEnabled =
                    false;

                PeriodComboBox.IsEnabled =
                    false;

                return;
            }

            MarketComboBox.IsEnabled =
                true;

            PeriodComboBox.IsEnabled =
                true;

            SynchronizeSelections();
        }


        private void ApiControlsVisibility()
        {
            Visibility visibility =
                _isImportedDataset
                    ? Visibility.Collapsed
                    : Visibility.Visible;

            MarketComboBox.Visibility =
                visibility;

            PeriodComboBox.Visibility =
                visibility;
        }


        private void SynchronizeSelections()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            _initialized =
                false;

            if (
                MarketComboBox.ItemsSource
                != store.Markets
            )
            {
                MarketComboBox.ItemsSource =
                    store.Markets;
            }

            if (
                PeriodComboBox.ItemsSource
                != store.Periods
            )
            {
                PeriodComboBox.ItemsSource =
                    store.Periods;
            }

            if (
                !string.IsNullOrWhiteSpace(
                    store.Pair
                )
            )
            {
                MarketComboBox.SelectedValue =
                    store.Pair;
            }

            if (store.PeriodDays > 0)
            {
                PeriodComboBox.SelectedValue =
                    store.PeriodDays;
            }

            _initialized =
                true;
        }


        // ==========================================
        // UPDATE VIEW
        // ==========================================

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
                    FindBrush(
                        "WarningBrush"
                    );

                ClearChart();

                return;
            }

            if (
                !string.IsNullOrWhiteSpace(
                    store.Error
                )
            )
            {
                ChartMarketText.Text =
                    $"{store.Pair} · Data unavailable";

                RefreshStatusText.Text =
                    "Unavailable";

                RefreshStatusIndicator.Fill =
                    FindBrush(
                        "NegativeBrush"
                    );

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
                    FindBrush(
                        "TextMutedBrush"
                    );

                VolumeMarketText.Text =
                    "—";

                ClearChart();

                return;
            }

            string intervalDescription =
                _isImportedDataset
                    ? GetImportedIntervalDescription(
                        store.Interval
                    )
                    : "Daily";

            ChartMarketText.Text =
                $"{store.Pair} · " +
                $"{store.Candles.Count} observations · " +
                intervalDescription;

            RefreshStatusText.Text =
                _isImportedDataset
                    ? "Imported"
                    : "Updated";

            RefreshStatusIndicator.Fill =
                FindBrush(
                    "PositiveBrush"
                );

            VolumeMarketText.Text =
                store.DataSource;

            DrawPriceChart(
                store.Candles
            );

            DrawVolumeChart(
                store.Candles
            );
        }


        // ==========================================
        // PRICE CHART
        // ==========================================

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

            const double verticalPadding =
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
                index++
            )
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


        // ==========================================
        // VOLUME CHART
        // ==========================================

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
                height <= 1 ||
                candles.Count == 0
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
                index++
            )
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
                            Math.Max(
                                0,
                                barHeight
                            ),

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


        // ==========================================
        // CLEAR
        // ==========================================

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


        // ==========================================
        // RESIZE
        // ==========================================

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


        // ==========================================
        // HELPERS
        // ==========================================

        private static string
            GetImportedIntervalDescription(
                int? interval)
        {
            if (!interval.HasValue)
            {
                return "Imported";
            }

            return interval.Value switch
            {
                1 =>
                    "1 Minute",

                5 =>
                    "5 Minutes",

                15 =>
                    "15 Minutes",

                30 =>
                    "30 Minutes",

                60 =>
                    "1 Hour",

                240 =>
                    "4 Hours",

                1440 =>
                    "Daily",

                10080 =>
                    "Weekly",

                21600 =>
                    "15 Days",

                _ =>
                    $"{interval.Value} Minutes"
            };
        }


        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }


        // ==========================================
        // UNLOAD
        // ==========================================

        private void ChartView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                StoreChanged;

            MarketComboBox.SelectionChanged -=
                SelectionChanged;

            PeriodComboBox.SelectionChanged -=
                SelectionChanged;
        }
    }
}
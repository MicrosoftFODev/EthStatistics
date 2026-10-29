using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly StatisticsService
            _statisticsService = new();


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public DashboardView()
        {
            InitializeComponent();

            Loaded +=
                DashboardView_Loaded;

            Unloaded +=
                DashboardView_Unloaded;

            MarketDataStore.Instance.Changed +=
                MarketDataStore_Changed;
        }


        // ==========================================
        // LOAD
        // ==========================================

        private void DashboardView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            RefreshDashboard();
        }


        // ==========================================
        // STORE CHANGED
        // ==========================================

        private void MarketDataStore_Changed(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Invoke(
                RefreshDashboard
            );
        }


        // ==========================================
        // REFRESH
        // ==========================================

        private void RefreshDashboard()
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            if (store.IsLoading)
            {
                MarketPairText.Text =
                    $"Loading {GetMarketDisplayName(store)}...";

                MarketStatusText.Text =
                    "Loading";

                MarketStatusIndicator.Fill =
                    FindBrush(
                        "WarningBrush"
                    );

                ClearDashboard();

                return;
            }


            if (
                !string.IsNullOrWhiteSpace(
                    store.Error
                )
            )
            {
                MarketPairText.Text =
                    "Market data unavailable";

                MarketStatusText.Text =
                    "Unavailable";

                MarketStatusIndicator.Fill =
                    FindBrush(
                        "NegativeBrush"
                    );

                ClearDashboard();

                return;
            }


            if (
                store.Candles.Count < 2 ||
                store.Interval is null
            )
            {
                MarketPairText.Text =
                    "Waiting for market data";

                MarketStatusText.Text =
                    "Waiting";

                MarketStatusIndicator.Fill =
                    FindBrush(
                        "TextMutedBrush"
                    );

                ClearDashboard();

                return;
            }


            try
            {
                StatisticsResult statistics =
                    _statisticsService.Calculate(
                        store.Candles,
                        store.Interval.Value,
                        store.ImportedReturns
                    );


                MarketCandle latest =
                    store.Candles[^1];


                MarketCandle previous =
                    store.Candles[^2];


                double change =
                    previous.Close > 0
                        ? (
                            latest.Close /
                            previous.Close
                        ) - 1.0
                        : 0.0;


                bool importedReturns =
                    store.ImportedValueType ==
                    ImportedValueType.PercentageReturn;


                bool isExcel =
                    store.DataSource.StartsWith(
                        "Excel",
                        StringComparison.OrdinalIgnoreCase
                    );


                // ==================================
                // METRICS
                // ==================================

                PriceLabel.Text =
                    importedReturns
                        ? "CUMULATIVE INDEX"
                        : "LATEST PRICE";


                PriceDescription.Text =
                    importedReturns
                        ? "Base index = 100"
                        : isExcel
                            ? "Latest observation"
                            : "Latest daily close";


                PriceValue.Text =
                    latest.Close.ToString(
                        "N2"
                    );


                ChangeValue.Text =
                    change.ToString(
                        "+0.00%;-0.00%;0.00%"
                    );


                ChangeValue.Foreground =
                    change > 0
                        ? FindBrush(
                            "PositiveBrush"
                        )
                        : change < 0
                            ? FindBrush(
                                "NegativeBrush"
                            )
                            : FindBrush(
                                "TextPrimaryBrush"
                            );


                ObservationValue.Text =
                    store.Candles.Count
                        .ToString();


                VolatilityValue.Text =
                    statistics
                        .AnnualizedVolatility
                        .ToString("P2");


                // ==================================
                // CURRENT DATASET
                // ==================================

                string marketName =
                    GetMarketDisplayName(
                        store
                    );


                string intervalName =
                    isExcel
                        ? GetIntervalLabel(
                            store.Interval.Value
                        )
                        : "Daily";


                MarketPairText.Text =
                    $"{marketName} · " +
                    $"{intervalName} · " +
                    $"{store.Candles.Count} observations";


                MarketStatusText.Text =
                    isExcel
                        ? "Imported"
                        : "Updated";


                MarketStatusIndicator.Fill =
                    FindBrush(
                        "PositiveBrush"
                    );


                DrawDashboardChart(
                    store.Candles
                );
            }
            catch
            {
                ClearDashboard();


                MarketStatusText.Text =
                    "Error";


                MarketStatusIndicator.Fill =
                    FindBrush(
                        "NegativeBrush"
                    );
            }
        }


        // ==========================================
        // MARKET DISPLAY NAME
        // ==========================================

        private static string GetMarketDisplayName(
            MarketDataStore store)
        {
            if (
                string.IsNullOrWhiteSpace(
                    store.Pair
                )
            )
            {
                return "Market";
            }


            KrakenMarket? market =
                store.Markets.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Pair,
                            store.Pair,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            if (
                market is not null &&
                !string.IsNullOrWhiteSpace(
                    market.DisplayName
                )
            )
            {
                return market.DisplayName;
            }


            return store.Pair;
        }


        // ==========================================
        // CHART
        // ==========================================

        private void DrawDashboardChart(
            IReadOnlyList<MarketCandle> candles)
        {
            double width =
                DashboardChartCanvas.ActualWidth;


            double height =
                DashboardChartCanvas.ActualHeight;


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
                return;
            }


            double minimum =
                values.Min();


            double maximum =
                values.Max();


            double range =
                maximum -
                minimum;


            if (range <= 0)
            {
                range =
                    1;
            }


            const double padding =
                8;


            double drawableHeight =
                Math.Max(
                    1,
                    height -
                    padding * 2
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
                    padding +
                    (
                        1 -
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


            DashboardPriceLine.Points =
                points;


            DashboardPriceLine.Visibility =
                Visibility.Visible;


            DashboardEmptyState.Visibility =
                Visibility.Collapsed;
        }


        private void DashboardChart_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            if (
                store.Candles.Count > 1
            )
            {
                DrawDashboardChart(
                    store.Candles
                );
            }
        }


        // ==========================================
        // CLEAR
        // ==========================================

        private void ClearDashboard()
        {
            PriceValue.Text =
                "—";


            ChangeValue.Text =
                "—";


            ObservationValue.Text =
                "—";


            VolatilityValue.Text =
                "—";


            ChangeValue.Foreground =
                FindBrush(
                    "TextPrimaryBrush"
                );


            DashboardPriceLine.Points =
                new PointCollection();


            DashboardPriceLine.Visibility =
                Visibility.Collapsed;


            DashboardEmptyState.Visibility =
                Visibility.Visible;
        }


        // ==========================================
        // HELPERS
        // ==========================================

        private static string GetIntervalLabel(
            int interval)
        {
            return interval switch
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
                    $"{interval} min"
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

        private void DashboardView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                MarketDataStore_Changed;
        }
    }
}
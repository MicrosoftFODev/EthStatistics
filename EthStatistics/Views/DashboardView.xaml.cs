using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class DashboardView : UserControl
    {
        // ==========================================
        // CONSTANTS
        // ==========================================

        private const int YAxisTickCount =
            5;

        private const double MinimumMonthLabelWidth =
            68.0;

        private const double ChartVerticalPadding =
            8.0;


        // ==========================================
        // SERVICES
        // ==========================================

        private readonly StatisticsService
            _statisticsService = new();


        // ==========================================
        // STATE
        // ==========================================

        private IReadOnlyList<MarketCandle>
            _displayedCandles =
                Array.Empty<MarketCandle>();


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
        // REFRESH DASHBOARD
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
                _displayedCandles =
                    store.Candles
                        .OrderBy(
                            candle =>
                                candle.Date
                        )
                        .ToList();


                UpdateMetrics(
                    store
                );


                UpdateMarketStatus(
                    store
                );


                DrawDashboardChart(
                    _displayedCandles
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
        // METRICS
        // ==========================================

        private void UpdateMetrics(
            MarketDataStore store)
        {
            StatisticsResult statistics =
                _statisticsService.Calculate(
                    store.Candles,
                    store.Interval!.Value,
                    store.ImportedReturns
                );


            MarketCandle latest =
                store.Candles[^1];


            MarketCandle previous =
                store.Candles[^2];


            double arithmeticReturn =
                previous.Close != 0
                    ? (
                        latest.Close -
                        previous.Close
                    )
                    /
                    previous.Close
                    : 0.0;


            bool importedReturns =
                store.ImportedValueType ==
                ImportedValueType.PercentageReturn;


            bool isExcel =
                IsExcelDataset(
                    store
                );


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
                FormatPrice(
                    latest.Close
                );


            ChangeLabel.Text =
                isExcel
                    ? "LATEST CHANGE"
                    : "DAILY CHANGE";


            ChangeDescription.Text =
                "Previous close to latest close";


            ChangeValue.Text =
                arithmeticReturn.ToString(
                    "+0.00%;-0.00%;0.00%",
                    CultureInfo.CurrentCulture
                );


            ChangeValue.Foreground =
                arithmeticReturn > 0
                    ? FindBrush(
                        "PositiveBrush"
                    )
                    : arithmeticReturn < 0
                        ? FindBrush(
                            "NegativeBrush"
                        )
                        : FindBrush(
                            "TextPrimaryBrush"
                        );


            ObservationValue.Text =
                store.Candles.Count.ToString(
                    "N0",
                    CultureInfo.CurrentCulture
                );


            VolatilityValue.Text =
                statistics
                    .AnnualizedVolatility
                    .ToString(
                        "P2",
                        CultureInfo.CurrentCulture
                    );
        }


        // ==========================================
        // STATUS
        // ==========================================

        private void UpdateMarketStatus(
            MarketDataStore store)
        {
            bool isExcel =
                IsExcelDataset(
                    store
                );


            string marketName =
                GetMarketDisplayName(
                    store
                );


            string intervalName =
                isExcel
                    ? GetIntervalLabel(
                        store.Interval!.Value
                    )
                    : "Daily";


            MarketPairText.Text =
                $"{marketName} · " +
                $"{intervalName} · " +
                $"{store.Candles.Count:N0} observations";


            MarketStatusText.Text =
                isExcel
                    ? "Imported"
                    : "Updated";


            MarketStatusIndicator.Fill =
                FindBrush(
                    "PositiveBrush"
                );
        }


        // ==========================================
        // DRAW CHART
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


            List<MarketCandle> validCandles =
                candles
                    .Where(
                        candle =>
                            double.IsFinite(
                                candle.Close
                            )
                    )
                    .OrderBy(
                        candle =>
                            candle.Date
                    )
                    .ToList();


            if (validCandles.Count < 2)
            {
                ClearDashboard();

                return;
            }


            double dataMinimum =
                validCandles.Min(
                    candle =>
                        candle.Close
                );


            double dataMaximum =
                validCandles.Max(
                    candle =>
                        candle.Close
                );


            PriceAxisScale axisScale =
                CreateNicePriceScale(
                    dataMinimum,
                    dataMaximum,
                    YAxisTickCount
                );


            DrawYAxis(
                axisScale
            );


            DrawPriceLine(
                validCandles,
                axisScale,
                width,
                height
            );


            DrawMonthlyAxis(
                validCandles,
                width,
                height
            );


            DashboardPriceLine.Visibility =
                Visibility.Visible;


            DashboardEmptyState.Visibility =
                Visibility.Collapsed;
        }


        // ==========================================
        // PRICE LINE
        // ==========================================

        private void DrawPriceLine(
            IReadOnlyList<MarketCandle> candles,
            PriceAxisScale axisScale,
            double width,
            double height)
        {
            double drawableHeight =
                Math.Max(
                    1,
                    height -
                    ChartVerticalPadding * 2
                );


            double axisRange =
                axisScale.Maximum -
                axisScale.Minimum;


            if (axisRange <= 0)
            {
                axisRange =
                    1;
            }


            var points =
                new PointCollection(
                    candles.Count
                );


            for (
                int index = 0;
                index < candles.Count;
                index++
            )
            {
                double x =
                    index *
                    width /
                    (candles.Count - 1);


                double normalizedPrice =
                    (
                        candles[index].Close -
                        axisScale.Minimum
                    )
                    /
                    axisRange;


                double y =
                    ChartVerticalPadding +
                    (
                        1.0 -
                        normalizedPrice
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
        }


        // ==========================================
        // Y-AXIS
        // ==========================================

        private void DrawYAxis(
            PriceAxisScale scale)
        {
            double[] values =
            {
                scale.Maximum,
                scale.Maximum - scale.Step,
                scale.Maximum - scale.Step * 2,
                scale.Maximum - scale.Step * 3,
                scale.Minimum
            };


            YAxisMaximumText.Text =
                FormatAxisPrice(
                    values[0],
                    scale.Step
                );


            YAxisUpperMiddleText.Text =
                FormatAxisPrice(
                    values[1],
                    scale.Step
                );


            YAxisMiddleText.Text =
                FormatAxisPrice(
                    values[2],
                    scale.Step
                );


            YAxisLowerMiddleText.Text =
                FormatAxisPrice(
                    values[3],
                    scale.Step
                );


            YAxisMinimumText.Text =
                FormatAxisPrice(
                    values[4],
                    scale.Step
                );
        }


        private static PriceAxisScale
            CreateNicePriceScale(
                double dataMinimum,
                double dataMaximum,
                int tickCount)
        {
            if (
                !double.IsFinite(
                    dataMinimum
                )
                ||
                !double.IsFinite(
                    dataMaximum
                )
            )
            {
                return new PriceAxisScale(
                    0,
                    1,
                    0.25
                );
            }


            if (dataMaximum < dataMinimum)
            {
                (
                    dataMinimum,
                    dataMaximum
                ) =
                (
                    dataMaximum,
                    dataMinimum
                );
            }


            double rawRange =
                dataMaximum -
                dataMinimum;


            if (rawRange <= 0)
            {
                double expansion =
                    Math.Abs(
                        dataMaximum
                    ) * 0.05;


                if (expansion <= 0)
                {
                    expansion =
                        1;
                }


                dataMinimum -=
                    expansion;


                dataMaximum +=
                    expansion;


                rawRange =
                    dataMaximum -
                    dataMinimum;
            }


            int intervalCount =
                Math.Max(
                    1,
                    tickCount - 1
                );


            double rawStep =
                rawRange /
                intervalCount;


            double niceStep =
                CalculateNiceStep(
                    rawStep
                );


            double niceMinimum =
                Math.Floor(
                    dataMinimum /
                    niceStep
                )
                *
                niceStep;


            double niceMaximum =
                Math.Ceiling(
                    dataMaximum /
                    niceStep
                )
                *
                niceStep;


            int actualIntervals =
                (int)Math.Ceiling(
                    (
                        niceMaximum -
                        niceMinimum
                    )
                    /
                    niceStep
                );


            if (actualIntervals < intervalCount)
            {
                niceMaximum =
                    niceMinimum +
                    niceStep *
                    intervalCount;
            }
            else if (actualIntervals > intervalCount)
            {
                niceStep =
                    CalculateNiceStep(
                        (
                            niceMaximum -
                            niceMinimum
                        )
                        /
                        intervalCount
                    );


                niceMinimum =
                    Math.Floor(
                        dataMinimum /
                        niceStep
                    )
                    *
                    niceStep;


                niceMaximum =
                    niceMinimum +
                    niceStep *
                    intervalCount;


                while (
                    niceMaximum <
                    dataMaximum
                )
                {
                    niceMinimum +=
                        -niceStep;


                    niceMaximum =
                        niceMinimum +
                        niceStep *
                        intervalCount;
                }
            }


            return new PriceAxisScale(
                niceMinimum,
                niceMaximum,
                niceStep
            );
        }


        private static double CalculateNiceStep(
            double rawStep)
        {
            if (
                !double.IsFinite(
                    rawStep
                )
                ||
                rawStep <= 0
            )
            {
                return 1;
            }


            double exponent =
                Math.Floor(
                    Math.Log10(
                        rawStep
                    )
                );


            double magnitude =
                Math.Pow(
                    10,
                    exponent
                );


            double normalizedStep =
                rawStep /
                magnitude;


            double niceNormalizedStep;


            if (normalizedStep <= 1)
            {
                niceNormalizedStep =
                    1;
            }
            else if (normalizedStep <= 2)
            {
                niceNormalizedStep =
                    2;
            }
            else if (normalizedStep <= 2.5)
            {
                niceNormalizedStep =
                    2.5;
            }
            else if (normalizedStep <= 5)
            {
                niceNormalizedStep =
                    5;
            }
            else
            {
                niceNormalizedStep =
                    10;
            }


            return
                niceNormalizedStep *
                magnitude;
        }


        // ==========================================
        // MONTHLY X-AXIS
        // ==========================================

        private void DrawMonthlyAxis(
            IReadOnlyList<MarketCandle> candles,
            double width,
            double graphHeight)
        {
            MonthlyGridCanvas.Children.Clear();

            MonthlyLabelsCanvas.Children.Clear();


            if (
                candles.Count < 2 ||
                width <= 1
            )
            {
                return;
            }


            List<MonthTick> allMonthTicks =
                CreateMonthTicks(
                    candles
                );


            if (allMonthTicks.Count == 0)
            {
                return;
            }


            int maximumVisibleLabelCount =
                Math.Max(
                    2,
                    (int)Math.Floor(
                        width /
                        MinimumMonthLabelWidth
                    )
                );


            int labelStep =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        allMonthTicks.Count /
                        (double)maximumVisibleLabelCount
                    )
                );


            List<MonthTick> visibleTicks =
                allMonthTicks
                    .Where(
                        (
                            tick,
                            index
                        ) =>
                            index % labelStep ==
                            0
                    )
                    .ToList();


            MonthTick lastMonthTick =
                allMonthTicks[^1];


            if (
                visibleTicks.Count == 0 ||
                visibleTicks[^1].Year !=
                    lastMonthTick.Year
                ||
                visibleTicks[^1].Month !=
                    lastMonthTick.Month
            )
            {
                visibleTicks.Add(
                    lastMonthTick
                );
            }


            bool multipleYears =
                candles[0].Date.Year !=
                candles[^1].Date.Year;


            foreach (
                MonthTick tick
                in visibleTicks
            )
            {
                double x =
                    tick.CandleIndex *
                    width /
                    (candles.Count - 1);


                DrawMonthlyGridLine(
                    x,
                    graphHeight
                );


                DrawMonthLabel(
                    tick,
                    x,
                    width,
                    multipleYears
                );
            }
        }


        private static List<MonthTick>
            CreateMonthTicks(
                IReadOnlyList<MarketCandle> candles)
        {
            var ticks =
                new List<MonthTick>();


            int? previousYear =
                null;


            int? previousMonth =
                null;


            for (
                int index = 0;
                index < candles.Count;
                index++
            )
            {
                DateTimeOffset date =
                    candles[index].Date;


                if (
                    previousYear ==
                        date.Year
                    &&
                    previousMonth ==
                        date.Month
                )
                {
                    continue;
                }


                ticks.Add(
                    new MonthTick(
                        index,
                        date.Year,
                        date.Month
                    )
                );


                previousYear =
                    date.Year;


                previousMonth =
                    date.Month;
            }


            return ticks;
        }


        private void DrawMonthlyGridLine(
            double x,
            double graphHeight)
        {
            var line =
                new Line
                {
                    X1 =
                        x,

                    X2 =
                        x,

                    Y1 =
                        0,

                    Y2 =
                        graphHeight,

                    Stroke =
                        FindBrush(
                            "BorderSubtleBrush"
                        ),

                    StrokeThickness =
                        1,

                    IsHitTestVisible =
                        false
                };


            MonthlyGridCanvas.Children.Add(
                line
            );
        }


        private void DrawMonthLabel(
            MonthTick tick,
            double x,
            double totalWidth,
            bool includeYear)
        {
            DateTime labelDate =
                new(
                    tick.Year,
                    tick.Month,
                    1
                );


            string label =
                includeYear
                    ? labelDate.ToString(
                        "MMM yy",
                        CultureInfo.CurrentCulture
                    )
                    : labelDate.ToString(
                        "MMM",
                        CultureInfo.CurrentCulture
                    );


            var textBlock =
                new TextBlock
                {
                    Text =
                        label,

                    Foreground =
                        FindBrush(
                            "TextMutedBrush"
                        ),

                    FontSize =
                        9,

                    IsHitTestVisible =
                        false
                };


            textBlock.Measure(
                new Size(
                    double.PositiveInfinity,
                    double.PositiveInfinity
                )
            );


            double labelWidth =
                textBlock.DesiredSize.Width;


            double left =
                x -
                labelWidth /
                2;


            left =
                Math.Max(
                    0,
                    Math.Min(
                        totalWidth -
                        labelWidth,
                        left
                    )
                );


            Canvas.SetLeft(
                textBlock,
                left
            );


            Canvas.SetTop(
                textBlock,
                7
            );


            MonthlyLabelsCanvas.Children.Add(
                textBlock
            );
        }


        // ==========================================
        // RESIZE
        // ==========================================

        private void DashboardChart_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (
                !_displayedCandles.Any()
            )
            {
                return;
            }


            DrawDashboardChart(
                _displayedCandles
            );
        }


        // ==========================================
        // DISPLAY HELPERS
        // ==========================================

        private static bool IsExcelDataset(
            MarketDataStore store)
        {
            return (
                store.DataSource
                ??
                ""
            ).StartsWith(
                "Excel",
                StringComparison.OrdinalIgnoreCase
            );
        }


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


        private static string FormatPrice(
            double value)
        {
            double absoluteValue =
                Math.Abs(
                    value
                );


            if (absoluteValue >= 1000)
            {
                return value.ToString(
                    "N2",
                    CultureInfo.CurrentCulture
                );
            }


            if (absoluteValue >= 1)
            {
                return value.ToString(
                    "N2",
                    CultureInfo.CurrentCulture
                );
            }


            return value.ToString(
                "N6",
                CultureInfo.CurrentCulture
            );
        }


        private static string FormatAxisPrice(
            double value,
            double step)
        {
            double absoluteStep =
                Math.Abs(
                    step
                );


            string format =
                absoluteStep >= 1
                    ? "N0"
                    : absoluteStep >= 0.01
                        ? "N2"
                        : "N6";


            return value.ToString(
                format,
                CultureInfo.CurrentCulture
            );
        }


        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }


        // ==========================================
        // CLEAR
        // ==========================================

        private void ClearDashboard()
        {
            _displayedCandles =
                Array.Empty<MarketCandle>();


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


            YAxisMaximumText.Text =
                "";


            YAxisUpperMiddleText.Text =
                "";


            YAxisMiddleText.Text =
                "";


            YAxisLowerMiddleText.Text =
                "";


            YAxisMinimumText.Text =
                "";


            MonthlyGridCanvas.Children.Clear();


            MonthlyLabelsCanvas.Children.Clear();
        }


        // ==========================================
        // CLEANUP
        // ==========================================

        private void DashboardView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                MarketDataStore_Changed;
        }


        // ==========================================
        // INTERNAL AXIS TYPES
        // ==========================================

        private sealed record PriceAxisScale(
            double Minimum,
            double Maximum,
            double Step
        );


        private sealed record MonthTick(
            int CandleIndex,
            int Year,
            int Month
        );
    }
}
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
    public partial class ChartView : UserControl
    {
        // ==========================================
        // CONSTANTS
        // ==========================================

        private const int DailyInterval =
            1440;

        private const int YAxisTickCount =
            5;

        private const double MinimumMonthLabelWidth =
            68.0;

        private const double ChartVerticalPadding =
            8.0;


        // ==========================================
        // USER SETTINGS
        // ==========================================

        private readonly string?
            _quoteCurrency;


        // ==========================================
        // STATE
        // ==========================================

        private bool
            _initialized;

        private bool
            _isImportedDataset;

        private IReadOnlyList<MarketCandle>
            _displayedCandles =
                Array.Empty<MarketCandle>();


        // ==========================================
        // EVENTS
        // ==========================================

        public event EventHandler<ChartPreferencesChangedEventArgs>?
            ChartPreferencesChanged;


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public ChartView(
            string? quoteCurrency)
        {
            _quoteCurrency =
                quoteCurrency?
                    .Trim()
                    .ToUpperInvariant();

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
        // AVAILABLE MARKETS
        // ==========================================

        private List<KrakenMarket> GetAvailableMarkets(
            MarketDataStore store)
        {
            IEnumerable<KrakenMarket> markets =
                store.Markets;


            if (
                !string.IsNullOrWhiteSpace(
                    _quoteCurrency
                )
            )
            {
                markets =
                    markets.Where(
                        market =>
                            string.Equals(
                                market.QuoteAsset,
                                _quoteCurrency,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );
            }


            return markets
                .OrderBy(
                    market =>
                        market.BaseAsset
                )
                .ThenBy(
                    market =>
                        market.DisplayName
                )
                .ToList();
        }


        private KrakenMarket? FindPreferredMarket(
            IReadOnlyList<KrakenMarket> availableMarkets,
            string? currentPair)
        {
            KrakenMarket? selectedMarket =
                null;


            // First try the active pair.

            if (
                !string.IsNullOrWhiteSpace(
                    currentPair
                )
            )
            {
                selectedMarket =
                    availableMarkets.FirstOrDefault(
                        market =>
                            string.Equals(
                                market.Pair,
                                currentPair,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );
            }


            // Prefer ETH with the selected
            // quote currency for a new profile.

            selectedMarket ??=
                availableMarkets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.BaseAsset,
                            "ETH",
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            // Otherwise use the first available pair.

            selectedMarket ??=
                availableMarkets.FirstOrDefault();


            return selectedMarket;
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


            List<KrakenMarket> availableMarkets =
                GetAvailableMarkets(
                    store
                );


            MarketComboBox.ItemsSource =
                availableMarkets;

            PeriodComboBox.ItemsSource =
                store.Periods;


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

                MarketComboBox.Visibility =
                    Visibility.Collapsed;

                PeriodComboBox.Visibility =
                    Visibility.Collapsed;

                _initialized =
                    true;

                return;
            }


            MarketComboBox.Visibility =
                Visibility.Visible;

            PeriodComboBox.Visibility =
                Visibility.Visible;

            MarketComboBox.IsEnabled =
                availableMarkets.Count > 0;

            PeriodComboBox.IsEnabled =
                store.Periods.Count > 0;


            KrakenMarket? selectedMarket =
                FindPreferredMarket(
                    availableMarkets,
                    store.Pair
                );


            if (selectedMarket is not null)
            {
                MarketComboBox.SelectedValue =
                    selectedMarket.Pair;
            }
            else
            {
                MarketComboBox.SelectedIndex =
                    -1;
            }


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
            else
            {
                PeriodComboBox.SelectedIndex =
                    -1;
            }


            _initialized =
                true;
        }


        // ==========================================
        // SELECTION
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

                    SynchronizeControls();

                    UpdateFromStore();
                }
            );
        }


        private void SynchronizeControls()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            _initialized =
                false;


            Visibility selectorVisibility =
                _isImportedDataset
                    ? Visibility.Collapsed
                    : Visibility.Visible;


            MarketComboBox.Visibility =
                selectorVisibility;

            PeriodComboBox.Visibility =
                selectorVisibility;


            if (_isImportedDataset)
            {
                MarketComboBox.IsEnabled =
                    false;

                PeriodComboBox.IsEnabled =
                    false;

                _initialized =
                    true;

                return;
            }


            List<KrakenMarket> availableMarkets =
                GetAvailableMarkets(
                    store
                );


            MarketComboBox.ItemsSource =
                availableMarkets;

            PeriodComboBox.ItemsSource =
                store.Periods;


            MarketComboBox.IsEnabled =
                availableMarkets.Count > 0;

            PeriodComboBox.IsEnabled =
                store.Periods.Count > 0;


            KrakenMarket? selectedMarket =
                FindPreferredMarket(
                    availableMarkets,
                    store.Pair
                );


            if (selectedMarket is not null)
            {
                MarketComboBox.SelectedValue =
                    selectedMarket.Pair;
            }
            else
            {
                MarketComboBox.SelectedIndex =
                    -1;
            }


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
            else
            {
                PeriodComboBox.SelectedIndex =
                    -1;
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
                    $"Loading {GetMarketDisplayName(store)}...";

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
                    $"{GetMarketDisplayName(store)} · " +
                    "Data unavailable";

                RefreshStatusText.Text =
                    "Unavailable";

                RefreshStatusIndicator.Fill =
                    FindBrush(
                        "NegativeBrush"
                    );

                ClearChart();

                return;
            }


            if (store.Candles.Count < 2)
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


            _displayedCandles =
                store.Candles
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


            if (_displayedCandles.Count < 2)
            {
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
                $"{GetMarketDisplayName(store)} · " +
                $"{_displayedCandles.Count:N0} observations · " +
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


            DrawCharts(
                _displayedCandles
            );
        }


        // ==========================================
        // DRAW CHARTS
        // ==========================================

        private void DrawCharts(
            IReadOnlyList<MarketCandle> candles)
        {
            DrawPriceChart(
                candles
            );

            DrawVolumeChart(
                candles
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


            double dataMinimum =
                candles.Min(
                    candle =>
                        candle.Close
                );


            double dataMaximum =
                candles.Max(
                    candle =>
                        candle.Close
                );


            PriceAxisScale scale =
                CreateNicePriceScale(
                    dataMinimum,
                    dataMaximum,
                    YAxisTickCount
                );


            DrawYAxis(
                scale
            );


            double range =
                scale.Maximum -
                scale.Minimum;


            if (range <= 0)
            {
                range =
                    1;
            }


            double drawableHeight =
                Math.Max(
                    1,
                    height -
                    ChartVerticalPadding * 2
                );


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
                        scale.Minimum
                    )
                    /
                    range;


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


            PricePolyline.Points =
                points;


            DrawMonthlyAxis(
                candles,
                width,
                PriceGraphArea.ActualHeight,
                PriceMonthlyGridCanvas,
                PriceMonthlyLabelsCanvas
            );


            PricePolyline.Visibility =
                Visibility.Visible;


            EmptyChartState.Visibility =
                Visibility.Collapsed;
        }


        // ==========================================
        // VOLUME CHART
        // ==========================================

        private void DrawVolumeChart(
            IReadOnlyList<MarketCandle> candles)
        {
            VolumeCanvas.Children.Clear();

            VolumeMonthlyGridCanvas.Children.Clear();


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
                ClearVolumeAxis();

                return;
            }


            double maximumVolume =
                candles
                    .Select(
                        candle =>
                            candle.Volume
                    )
                    .Where(
                        volume =>
                            double.IsFinite(
                                volume
                            )
                            &&
                            volume >= 0
                    )
                    .DefaultIfEmpty(0)
                    .Max();


            if (maximumVolume <= 0)
            {
                ClearVolumeAxis();

                EmptyVolumeState.Visibility =
                    Visibility.Visible;

                return;
            }


            // Top = 100% of displayed maximum.
            // Middle = 50% of displayed maximum.
            // Bottom = 0.

            VolumeMaximumText.Text =
                FormatVolume(
                    maximumVolume
                );


            VolumeMiddleText.Text =
                FormatVolume(
                    maximumVolume *
                    0.50
                );


            VolumeMinimumText.Text =
                "0";


            double slotWidth =
                width /
                candles.Count;


            double barWidth =
                Math.Max(
                    1,
                    slotWidth *
                    0.75
                );


            for (
                int index = 0;
                index < candles.Count;
                index++
            )
            {
                MarketCandle candle =
                    candles[index];


                double volume =
                    double.IsFinite(
                        candle.Volume
                    )
                        ? Math.Max(
                            0,
                            candle.Volume
                        )
                        : 0;


                double normalizedVolume =
                    volume /
                    maximumVolume;


                double barHeight =
                    normalizedVolume *
                    height;


                Brush barBrush;


                if (candle.Close > candle.Open)
                {
                    barBrush =
                        FindBrush(
                            "PositiveBrush"
                        );
                }
                else if (candle.Close < candle.Open)
                {
                    barBrush =
                        FindBrush(
                            "NegativeBrush"
                        );
                }
                else
                {
                    barBrush =
                        FindBrush(
                            "TextMutedBrush"
                        );
                }


                var rectangle =
                    new Rectangle
                    {
                        Width =
                            barWidth,

                        Height =
                            Math.Max(
                                1,
                                barHeight
                            ),

                        Fill =
                            barBrush,

                        Opacity =
                            0.75,

                        ToolTip =
                            CreateVolumeToolTip(
                                candle
                            )
                    };


                double x =
                    index *
                    slotWidth
                    +
                    (
                        slotWidth -
                        barWidth
                    )
                    /
                    2.0;


                Canvas.SetLeft(
                    rectangle,
                    x
                );


                Canvas.SetTop(
                    rectangle,
                    height -
                    barHeight
                );


                VolumeCanvas.Children.Add(
                    rectangle
                );
            }


            DrawMonthlyGridLinesOnly(
                candles,
                width,
                height,
                VolumeMonthlyGridCanvas
            );


            EmptyVolumeState.Visibility =
                Visibility.Collapsed;
        }


        private static string CreateVolumeToolTip(
            MarketCandle candle)
        {
            string direction =
                candle.Close > candle.Open
                    ? "Bullish"
                    : candle.Close < candle.Open
                        ? "Bearish"
                        : "Unchanged";


            return
                $"{candle.Date:dd MMM yyyy}\n" +
                $"Volume: {FormatVolume(candle.Volume)}\n" +
                $"Open: {candle.Open:N2}\n" +
                $"Close: {candle.Close:N2}\n" +
                $"Direction: {direction}";
        }


        private void ClearVolumeAxis()
        {
            VolumeMaximumText.Text =
                "";

            VolumeMiddleText.Text =
                "";

            VolumeMinimumText.Text =
                "0";
        }


        // ==========================================
        // PRICE Y-AXIS
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
                    ) *
                    0.05;


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
                niceMinimum +
                niceStep *
                intervalCount;


            while (
                niceMaximum <
                dataMaximum
            )
            {
                niceMaximum +=
                    niceStep;

                niceMinimum =
                    niceMaximum -
                    niceStep *
                    intervalCount;
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


            double normalized =
                rawStep /
                magnitude;


            double niceNormalized;


            if (normalized <= 1)
            {
                niceNormalized =
                    1;
            }
            else if (normalized <= 2)
            {
                niceNormalized =
                    2;
            }
            else if (normalized <= 2.5)
            {
                niceNormalized =
                    2.5;
            }
            else if (normalized <= 5)
            {
                niceNormalized =
                    5;
            }
            else
            {
                niceNormalized =
                    10;
            }


            return
                niceNormalized *
                magnitude;
        }


        // ==========================================
        // MONTHLY AXIS
        // ==========================================

        private void DrawMonthlyAxis(
            IReadOnlyList<MarketCandle> candles,
            double width,
            double graphHeight,
            Canvas gridCanvas,
            Canvas labelsCanvas)
        {
            gridCanvas.Children.Clear();

            labelsCanvas.Children.Clear();


            List<MonthTick> allTicks =
                CreateMonthTicks(
                    candles
                );


            List<MonthTick> visibleTicks =
                SelectVisibleMonthTicks(
                    allTicks,
                    width
                );


            bool multipleYears =
                candles[0].Date.Year !=
                candles[^1].Date.Year;


            foreach (
                MonthTick tick
                in visibleTicks
            )
            {
                double x =
                    GetTickXPosition(
                        tick,
                        candles.Count,
                        width
                    );


                AddVerticalGridLine(
                    gridCanvas,
                    x,
                    graphHeight
                );


                AddMonthLabel(
                    labelsCanvas,
                    tick,
                    x,
                    width,
                    multipleYears
                );
            }
        }


        private void DrawMonthlyGridLinesOnly(
            IReadOnlyList<MarketCandle> candles,
            double width,
            double graphHeight,
            Canvas gridCanvas)
        {
            gridCanvas.Children.Clear();


            List<MonthTick> ticks =
                SelectVisibleMonthTicks(
                    CreateMonthTicks(
                        candles
                    ),
                    width
                );


            foreach (
                MonthTick tick
                in ticks
            )
            {
                double x =
                    GetTickXPosition(
                        tick,
                        candles.Count,
                        width
                    );


                AddVerticalGridLine(
                    gridCanvas,
                    x,
                    graphHeight
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


        private static List<MonthTick>
            SelectVisibleMonthTicks(
                IReadOnlyList<MonthTick> allTicks,
                double width)
        {
            if (allTicks.Count == 0)
            {
                return new List<MonthTick>();
            }


            int maximumLabels =
                Math.Max(
                    2,
                    (int)Math.Floor(
                        width /
                        MinimumMonthLabelWidth
                    )
                );


            int step =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        allTicks.Count /
                        (double)maximumLabels
                    )
                );


            List<MonthTick> visible =
                allTicks
                    .Where(
                        (
                            tick,
                            index
                        ) =>
                            index % step ==
                            0
                    )
                    .ToList();


            MonthTick last =
                allTicks[^1];


            if (
                visible.Count == 0 ||
                visible[^1].Year !=
                    last.Year
                ||
                visible[^1].Month !=
                    last.Month
            )
            {
                visible.Add(
                    last
                );
            }


            return visible;
        }


        private static double GetTickXPosition(
            MonthTick tick,
            int candleCount,
            double width)
        {
            if (candleCount <= 1)
            {
                return 0;
            }


            return
                tick.CandleIndex *
                width /
                (candleCount - 1);
        }


        private void AddVerticalGridLine(
            Canvas canvas,
            double x,
            double height)
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
                        height,

                    Stroke =
                        FindBrush(
                            "BorderSubtleBrush"
                        ),

                    StrokeThickness =
                        1,

                    IsHitTestVisible =
                        false
                };


            canvas.Children.Add(
                line
            );
        }


        private void AddMonthLabel(
            Canvas canvas,
            MonthTick tick,
            double x,
            double totalWidth,
            bool includeYear)
        {
            DateTime date =
                new(
                    tick.Year,
                    tick.Month,
                    1
                );


            string label =
                includeYear
                    ? date.ToString(
                        "MMM yy",
                        CultureInfo.CurrentCulture
                    )
                    : date.ToString(
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


            canvas.Children.Add(
                textBlock
            );
        }


        // ==========================================
        // RESIZE
        // ==========================================

        private void ChartContainer_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            if (_displayedCandles.Count < 2)
            {
                return;
            }


            DrawCharts(
                _displayedCandles
            );
        }


        // ==========================================
        // CLEAR
        // ==========================================

        private void ClearChart()
        {
            _displayedCandles =
                Array.Empty<MarketCandle>();


            PricePolyline.Points =
                new PointCollection();


            PricePolyline.Visibility =
                Visibility.Collapsed;


            EmptyChartState.Visibility =
                Visibility.Visible;


            VolumeCanvas.Children.Clear();


            EmptyVolumeState.Visibility =
                Visibility.Visible;


            PriceMonthlyGridCanvas.Children.Clear();

            PriceMonthlyLabelsCanvas.Children.Clear();

            VolumeMonthlyGridCanvas.Children.Clear();


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


            ClearVolumeAxis();
        }


        // ==========================================
        // DISPLAY HELPERS
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


            return
                !string.IsNullOrWhiteSpace(
                    market?.DisplayName
                )
                    ? market.DisplayName
                    : store.Pair;
        }


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


        private static string FormatVolume(
            double value)
        {
            double absoluteValue =
                Math.Abs(
                    value
                );


            if (absoluteValue >= 1_000_000_000)
            {
                return (
                    value /
                    1_000_000_000
                ).ToString(
                    "0.##",
                    CultureInfo.CurrentCulture
                )
                + "B";
            }


            if (absoluteValue >= 1_000_000)
            {
                return (
                    value /
                    1_000_000
                ).ToString(
                    "0.##",
                    CultureInfo.CurrentCulture
                )
                + "M";
            }


            if (absoluteValue >= 1_000)
            {
                return (
                    value /
                    1_000
                ).ToString(
                    "0.##",
                    CultureInfo.CurrentCulture
                )
                + "K";
            }


            return value.ToString(
                "N0",
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


        // ==========================================
        // INTERNAL TYPES
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
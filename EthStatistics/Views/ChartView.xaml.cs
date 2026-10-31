using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class ChartView : UserControl
    {
        private const int DailyInterval = 1440;
        private const int YAxisTickCount = 5;
        private const int RsiPeriod = 14;
        private const int StochasticPeriod = 14;
        private const int ConnorsPricePeriod = 3;
        private const int ConnorsStreakPeriod = 2;
        private const int ConnorsRankPeriod = 20;
        private const double MinimumMonthLabelWidth = 68.0;
        private const double ChartVerticalPadding = 8.0;

        private readonly string? _quoteCurrency;
        private readonly StatisticsService _statisticsService = new();
        private readonly Dictionary<string, double> _selectedMeans = new();
        private readonly Dictionary<string, double[]> _selectedRsis = new();

        private readonly Brush _arithmeticBrush = MakeBrush(255, 193, 7);
        private readonly Brush _geometricBrush = MakeBrush(156, 39, 176);
        private readonly Brush _harmonicBrush = MakeBrush(255, 112, 67);
        private readonly Brush _weightedBrush = MakeBrush(0, 188, 212);
        private readonly Brush _wilderBrush = MakeBrush(33, 150, 243);
        private readonly Brush _cutlerBrush = MakeBrush(156, 39, 176);
        private readonly Brush _exponentialBrush = MakeBrush(255, 193, 7);
        private readonly Brush _stochasticBrush = MakeBrush(0, 188, 212);
        private readonly Brush _connorsBrush = MakeBrush(255, 152, 0);

        private bool _initialized;
        private bool _isImportedDataset;
        private bool _storeSubscribed;
        private IReadOnlyList<MarketCandle> _displayedCandles = Array.Empty<MarketCandle>();

        public event EventHandler<ChartPreferencesChangedEventArgs>? ChartPreferencesChanged;

        public ChartView(string? quoteCurrency)
        {
            _quoteCurrency = quoteCurrency?.Trim().ToUpperInvariant();
            InitializeComponent();
            Loaded += ChartView_Loaded;
            Unloaded += ChartView_Unloaded;
            MarketComboBox.SelectionChanged += SelectionChanged;
            PeriodComboBox.SelectionChanged += SelectionChanged;
        }

        private static Brush MakeBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private void ChartView_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_storeSubscribed)
            {
                MarketDataStore.Instance.Changed += StoreChanged;
                _storeSubscribed = true;
            }

            UpdateImportState();
            PopulateFromStore();
            UpdateFromStore();
        }

        private void UpdateImportState()
        {
            _isImportedDataset = (MarketDataStore.Instance.DataSource ?? string.Empty)
                .StartsWith("Excel", StringComparison.OrdinalIgnoreCase);
        }

        private List<KrakenMarket> GetAvailableMarkets(MarketDataStore store)
        {
            IEnumerable<KrakenMarket> markets = store.Markets;
            if (!string.IsNullOrWhiteSpace(_quoteCurrency))
            {
                markets = markets.Where(m => string.Equals(m.QuoteAsset, _quoteCurrency, StringComparison.OrdinalIgnoreCase));
            }

            return markets.OrderBy(m => m.BaseAsset).ThenBy(m => m.DisplayName).ToList();
        }

        private static KrakenMarket? FindPreferredMarket(IReadOnlyList<KrakenMarket> markets, string? pair)
        {
            return markets.FirstOrDefault(m => string.Equals(m.Pair, pair, StringComparison.OrdinalIgnoreCase))
                ?? markets.FirstOrDefault(m => string.Equals(m.BaseAsset, "ETH", StringComparison.OrdinalIgnoreCase))
                ?? markets.FirstOrDefault();
        }

        private void PopulateFromStore() => SynchronizeControls();

        private void SynchronizeControls()
        {
            MarketDataStore store = MarketDataStore.Instance;
            _initialized = false;
            Visibility visibility = _isImportedDataset ? Visibility.Collapsed : Visibility.Visible;
            MarketComboBox.Visibility = visibility;
            PeriodComboBox.Visibility = visibility;

            if (_isImportedDataset)
            {
                MarketComboBox.IsEnabled = false;
                PeriodComboBox.IsEnabled = false;
                _initialized = true;
                return;
            }

            List<KrakenMarket> markets = GetAvailableMarkets(store);
            MarketComboBox.ItemsSource = markets;
            PeriodComboBox.ItemsSource = store.Periods;
            MarketComboBox.IsEnabled = markets.Count > 0;
            PeriodComboBox.IsEnabled = store.Periods.Count > 0;
            MarketComboBox.SelectedValue = FindPreferredMarket(markets, store.Pair)?.Pair;

            MarketPeriod? period = store.Periods.FirstOrDefault(p => p.Days == store.PeriodDays)
                ?? store.Periods.FirstOrDefault(p => p.IsDefault)
                ?? store.Periods.FirstOrDefault();
            PeriodComboBox.SelectedValue = period?.Days;
            _initialized = true;
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_initialized || _isImportedDataset ||
                MarketComboBox.SelectedValue is not string pair ||
                PeriodComboBox.SelectedValue is not int days)
            {
                return;
            }

            ChartPreferencesChanged?.Invoke(this, new ChartPreferencesChangedEventArgs
            {
                Pair = pair,
                Interval = DailyInterval,
                PeriodDays = days
            });
        }

        private void IndicatorSelectionChanged(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || _displayedCandles.Count < 2) return;
            DrawPriceChart(_displayedCandles);
            DrawRsiChart(_displayedCandles);
        }

        private void StoreChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => StoreChanged(sender, e));
                return;
            }

            UpdateImportState();
            SynchronizeControls();
            UpdateFromStore();
        }

        private void UpdateFromStore()
        {
            MarketDataStore store = MarketDataStore.Instance;
            if (store.IsLoading)
            {
                ChartMarketText.Text = $"Loading {GetMarketDisplayName(store)}...";
                RefreshStatusText.Text = "Refreshing";
                RefreshStatusIndicator.Fill = FindBrush("WarningBrush");
                ClearChart();
                return;
            }

            if (!string.IsNullOrWhiteSpace(store.Error))
            {
                ChartMarketText.Text = $"{GetMarketDisplayName(store)} · Data unavailable";
                RefreshStatusText.Text = "Unavailable";
                RefreshStatusIndicator.Fill = FindBrush("NegativeBrush");
                ClearChart();
                return;
            }

            _displayedCandles = store.Candles
                .Where(c => double.IsFinite(c.Close) && c.Close > 0.0)
                .OrderBy(c => c.Date)
                .ToList();

            if (_displayedCandles.Count < 2)
            {
                ChartMarketText.Text = "Waiting for market data";
                RefreshStatusText.Text = "Waiting";
                RefreshStatusIndicator.Fill = FindBrush("TextMutedBrush");
                ClearChart();
                return;
            }

            string interval = _isImportedDataset ? GetImportedIntervalDescription(store.Interval) : "Daily";
            ChartMarketText.Text = $"{GetMarketDisplayName(store)} · {_displayedCandles.Count:N0} observations · {interval}";
            RefreshStatusText.Text = _isImportedDataset ? "Imported" : "Updated";
            RefreshStatusIndicator.Fill = FindBrush("PositiveBrush");
            VolumeMarketText.Text = store.DataSource;
            DrawCharts(_displayedCandles);
        }

        private void DrawCharts(IReadOnlyList<MarketCandle> candles)
        {
            DrawPriceChart(candles);
            DrawVolumeChart(candles);
            DrawRsiChart(candles);
        }

        private void DrawPriceChart(IReadOnlyList<MarketCandle> candles)
        {
            double width = PriceCanvas.ActualWidth;
            double height = PriceCanvas.ActualHeight;
            if (width <= 1.0 || height <= 1.0 || candles.Count < 2) return;

            PriceAxisScale scale = CreateNicePriceScale(candles.Min(c => c.Close), candles.Max(c => c.Close), YAxisTickCount);
            DrawYAxis(scale);
            double range = Math.Max(1e-12, scale.Maximum - scale.Minimum);
            double drawableHeight = Math.Max(1.0, height - ChartVerticalPadding * 2.0);
            var points = new PointCollection(candles.Count);

            for (int i = 0; i < candles.Count; i++)
            {
                double x = GetDataX(i, width, candles.Count);
                double y = ChartVerticalPadding + (1.0 - (candles[i].Close - scale.Minimum) / range) * drawableHeight;
                points.Add(new Point(x, y));
            }

            PricePolyline.Points = points;
            DrawSelectedMeans(candles, scale, width, height);
            DrawMonthlyAxis(candles, width, PriceGraphArea.ActualHeight, PriceMonthlyGridCanvas, PriceMonthlyLabelsCanvas);
            PricePolyline.Visibility = Visibility.Visible;
            EmptyChartState.Visibility = Visibility.Collapsed;
        }

        private void DrawSelectedMeans(IReadOnlyList<MarketCandle> candles, PriceAxisScale scale, double width, double height)
        {
            MeanLinesCanvas.Children.Clear();
            MeanLegendPanel.Children.Clear();
            _selectedMeans.Clear();

            if (ArithmeticMeanCheckBox.IsChecked == true)
                DrawMean("Arithmetic", _statisticsService.CalculateArithmeticMeanPrice(candles), _arithmeticBrush, scale, width, height);
            if (GeometricMeanCheckBox.IsChecked == true)
                DrawMean("Geometric", _statisticsService.CalculateGeometricMeanPrice(candles), _geometricBrush, scale, width, height);
            if (HarmonicMeanCheckBox.IsChecked == true)
                DrawMean("Harmonic", _statisticsService.CalculateHarmonicMeanPrice(candles), _harmonicBrush, scale, width, height);
            if (WeightedMeanCheckBox.IsChecked == true)
                DrawMean("Volume weighted", _statisticsService.CalculateVolumeWeightedMeanPrice(candles), _weightedBrush, scale, width, height);
        }

        private void DrawMean(string name, double value, Brush brush, PriceAxisScale scale, double width, double height)
        {
            if (!double.IsFinite(value)) return;
            _selectedMeans[name] = value;
            double range = Math.Max(1e-12, scale.Maximum - scale.Minimum);
            double drawableHeight = Math.Max(1.0, height - ChartVerticalPadding * 2.0);
            double y = ChartVerticalPadding + (1.0 - (value - scale.Minimum) / range) * drawableHeight;
            MeanLinesCanvas.Children.Add(new Line
            {
                X1 = 0,
                X2 = width,
                Y1 = y,
                Y2 = y,
                Stroke = brush,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 5, 3 },
                IsHitTestVisible = false
            });
            AddLegend(MeanLegendPanel, $"{name}: {FormatPrice(value)}", brush);
        }

        private void DrawVolumeChart(IReadOnlyList<MarketCandle> candles)
        {
            VolumeCanvas.Children.Clear();
            VolumeMonthlyGridCanvas.Children.Clear();
            double width = VolumeCanvas.ActualWidth;
            double height = VolumeCanvas.ActualHeight;
            if (width <= 1.0 || height <= 1.0) return;

            double maximum = candles.Select(c => c.Volume).Where(v => double.IsFinite(v) && v >= 0).DefaultIfEmpty(0).Max();
            if (maximum <= 0)
            {
                ClearVolumeAxis();
                EmptyVolumeState.Visibility = Visibility.Visible;
                return;
            }

            VolumeMaximumText.Text = FormatVolume(maximum);
            VolumeMiddleText.Text = FormatVolume(maximum * 0.5);
            VolumeMinimumText.Text = "0";
            double slot = width / candles.Count;
            double barWidth = Math.Max(1, slot * 0.75);

            for (int i = 0; i < candles.Count; i++)
            {
                MarketCandle candle = candles[i];
                double volume = double.IsFinite(candle.Volume) ? Math.Max(0, candle.Volume) : 0;
                double barHeight = volume / maximum * height;
                Brush brush = candle.Close > candle.Open ? FindBrush("PositiveBrush")
                    : candle.Close < candle.Open ? FindBrush("NegativeBrush") : FindBrush("TextMutedBrush");
                var rect = new Rectangle { Width = barWidth, Height = Math.Max(1, barHeight), Fill = brush, Opacity = 0.75 };
                Canvas.SetLeft(rect, i * slot + (slot - barWidth) / 2.0);
                Canvas.SetTop(rect, height - barHeight);
                VolumeCanvas.Children.Add(rect);
            }

            DrawMonthlyGridLinesOnly(candles, width, height, VolumeMonthlyGridCanvas);
            EmptyVolumeState.Visibility = Visibility.Collapsed;
        }

        private void DrawRsiChart(IReadOnlyList<MarketCandle> candles)
        {
            RsiSeriesCanvas.Children.Clear();
            RsiReferenceCanvas.Children.Clear();
            RsiLegendPanel.Children.Clear();
            _selectedRsis.Clear();
            double width = RsiSeriesCanvas.ActualWidth;
            double height = RsiSeriesCanvas.ActualHeight;
            if (width <= 1.0 || height <= 1.0) return;

            DrawRsiReferenceLine(70, width, height);
            DrawRsiReferenceLine(50, width, height);
            DrawRsiReferenceLine(30, width, height);
            bool any = false;

            if (WilderRsiCheckBox.IsChecked == true)
                any |= DrawRsiSeries("Wilder", _statisticsService.CalculateWilderRsi(candles, RsiPeriod), candles.Count, width, height, _wilderBrush);
            if (CutlerRsiCheckBox.IsChecked == true)
                any |= DrawRsiSeries("Cutler", _statisticsService.CalculateCutlerRsi(candles, RsiPeriod), candles.Count, width, height, _cutlerBrush);
            if (ExponentialRsiCheckBox.IsChecked == true)
                any |= DrawRsiSeries("Exponential", _statisticsService.CalculateExponentialRsi(candles, RsiPeriod), candles.Count, width, height, _exponentialBrush);
            if (StochasticRsiCheckBox.IsChecked == true)
                any |= DrawRsiSeries("Stochastic", _statisticsService.CalculateStochasticRsi(candles, RsiPeriod, StochasticPeriod), candles.Count, width, height, _stochasticBrush);
            if (ConnorsRsiCheckBox.IsChecked == true)
                any |= DrawRsiSeries("Connors", _statisticsService.CalculateConnorsRsi(candles, ConnorsPricePeriod, ConnorsStreakPeriod, ConnorsRankPeriod), candles.Count, width, height, _connorsBrush);

            EmptyRsiState.Visibility = any ? Visibility.Collapsed : Visibility.Visible;
        }

        private bool DrawRsiSeries(string name, double[] values, int count, double width, double height, Brush neutral)
        {
            if (values.Length != count || values.Length < 2) return false;
            _selectedRsis[name] = values;
            bool drawn = false;
            for (int i = 1; i < values.Length; i++)
            {
                double a = values[i - 1], b = values[i];
                if (!double.IsFinite(a) || !double.IsFinite(b)) continue;
                a = Math.Clamp(a, 0, 100); b = Math.Clamp(b, 0, 100);
                DrawRsiSegment(GetDataX(i - 1, width, count), RsiY(a, height), GetDataX(i, width, count), RsiY(b, height), a, b, neutral);
                drawn = true;
            }
            if (drawn) AddLegend(RsiLegendPanel, name, neutral);
            return drawn;
        }

        private void DrawRsiSegment(double x1, double y1, double x2, double y2, double a, double b, Brush neutral)
        {
            var cuts = new List<double>();
            if (Crosses(a, b, 30)) cuts.Add(30);
            if (Crosses(a, b, 70)) cuts.Add(70);
            cuts = b >= a ? cuts.OrderBy(x => x).ToList() : cuts.OrderByDescending(x => x).ToList();
            double sx = x1, sy = y1, sv = a, delta = b - a;
            foreach (double cut in cuts)
            {
                if (Math.Abs(delta) <= double.Epsilon) break;
                double f = (cut - a) / delta;
                double cx = x1 + f * (x2 - x1), cy = y1 + f * (y2 - y1);
                AddRsiLine(sx, sy, cx, cy, RsiBrush((sv + cut) / 2, neutral));
                sx = cx; sy = cy; sv = cut;
            }
            AddRsiLine(sx, sy, x2, y2, RsiBrush((sv + b) / 2, neutral));
        }

        private static bool Crosses(double a, double b, double level) =>
            (a < level && b >= level) || (a > level && b <= level);

        private Brush RsiBrush(double value, Brush neutral) => value > 70 ? FindBrush("NegativeBrush") : value < 30 ? FindBrush("PositiveBrush") : neutral;

        private void AddRsiLine(double x1, double y1, double x2, double y2, Brush brush) =>
            RsiSeriesCanvas.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = 1.8, IsHitTestVisible = false });

        private void DrawRsiReferenceLine(double value, double width, double height)
        {
            var line = new Line { X1 = 0, X2 = width, Y1 = RsiY(value, height), Y2 = RsiY(value, height), Stroke = FindBrush("BorderSubtleBrush"), StrokeThickness = 1, IsHitTestVisible = false };
            if (value != 50) line.StrokeDashArray = new DoubleCollection { 4, 4 };
            RsiReferenceCanvas.Children.Add(line);
        }

        private static double RsiY(double value, double height) => (1.0 - value / 100.0) * height;

        private void PriceChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryHover(PriceHoverCanvas, e, out Point mouse, out int index, out double x)) return;
            MarketCandle candle = _displayedCandles[index];
            DrawCrosshair(PriceHoverCanvas, x, GetPriceY(candle.Close), FindBrush("AccentBrush"));
            var lines = new List<string>
            {
                candle.Date.ToString("dd MMM yyyy", CultureInfo.CurrentCulture),
                $"Open: {FormatPrice(candle.Open)}", $"High: {FormatPrice(candle.High)}",
                $"Low: {FormatPrice(candle.Low)}", $"Close: {FormatPrice(candle.Close)}"
            };
            lines.AddRange(_selectedMeans.Select(m => $"{m.Key}: {FormatPrice(m.Value)}"));
            PriceHoverText.Text = string.Join(Environment.NewLine, lines);
            ShowTip(PriceHoverToolTip, mouse, PriceHoverCanvas.ActualWidth, PriceHoverCanvas.ActualHeight);
        }

        private void VolumeChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryHover(VolumeHoverCanvas, e, out Point mouse, out int index, out double x)) return;
            MarketCandle c = _displayedCandles[index];
            DrawCrosshair(VolumeHoverCanvas, x, mouse.Y, FindBrush("TextSecondaryBrush"));
            string direction = c.Close > c.Open ? "Bullish" : c.Close < c.Open ? "Bearish" : "Unchanged";
            VolumeHoverText.Text = $"{c.Date:dd MMM yyyy}\nVolume: {FormatVolume(c.Volume)}\nOpen: {FormatPrice(c.Open)}\nClose: {FormatPrice(c.Close)}\nDirection: {direction}";
            ShowTip(VolumeHoverToolTip, mouse, VolumeHoverCanvas.ActualWidth, VolumeHoverCanvas.ActualHeight);
        }

        private void RsiChart_MouseMove(object sender, MouseEventArgs e)
        {
            if (!TryHover(RsiHoverCanvas, e, out Point mouse, out int index, out double x)) return;
            var values = _selectedRsis.Where(s => index < s.Value.Length && double.IsFinite(s.Value[index])).ToList();
            double markerY = values.Count > 0 ? RsiY(values[0].Value[index], RsiHoverCanvas.ActualHeight) : mouse.Y;
            DrawCrosshair(RsiHoverCanvas, x, markerY, FindBrush("TextSecondaryBrush"));
            var lines = new List<string> { _displayedCandles[index].Date.ToString("dd MMM yyyy", CultureInfo.CurrentCulture) };
            foreach (var series in _selectedRsis)
                lines.Add(index < series.Value.Length && double.IsFinite(series.Value[index]) ? $"{series.Key}: {series.Value[index]:N2}" : $"{series.Key}: —");
            RsiHoverText.Text = string.Join(Environment.NewLine, lines);
            ShowTip(RsiHoverToolTip, mouse, RsiHoverCanvas.ActualWidth, RsiHoverCanvas.ActualHeight);
        }

        private bool TryHover(Canvas canvas, MouseEventArgs e, out Point mouse, out int index, out double x)
        {
            mouse = e.GetPosition(canvas); index = 0; x = 0;
            if (_displayedCandles.Count == 0 || canvas.ActualWidth <= 1) return false;
            index = Math.Clamp((int)Math.Round(mouse.X / canvas.ActualWidth * (_displayedCandles.Count - 1.0)), 0, _displayedCandles.Count - 1);
            x = GetDataX(index, canvas.ActualWidth, _displayedCandles.Count);
            return true;
        }

        private double GetPriceY(double value)
        {
            double min = _displayedCandles.Min(c => c.Close), max = _displayedCandles.Max(c => c.Close);
            PriceAxisScale scale = CreateNicePriceScale(min, max, YAxisTickCount);
            double range = Math.Max(1e-12, scale.Maximum - scale.Minimum);
            return ChartVerticalPadding + (1 - (value - scale.Minimum) / range) * Math.Max(1, PriceHoverCanvas.ActualHeight - 2 * ChartVerticalPadding);
        }

        private static void DrawCrosshair(Canvas canvas, double x, double y, Brush brush)
        {
            canvas.Children.Clear();
            canvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = canvas.ActualHeight, Stroke = brush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 3, 3 }, IsHitTestVisible = false });
            var dot = new Ellipse { Width = 8, Height = 8, Fill = brush, Stroke = Brushes.White, StrokeThickness = 1, IsHitTestVisible = false };
            Canvas.SetLeft(dot, x - 4); Canvas.SetTop(dot, Math.Clamp(y - 4, 0, Math.Max(0, canvas.ActualHeight - 8))); canvas.Children.Add(dot);
        }

        private static void ShowTip(Border tip, Point mouse, double width, double height)
        {
            double left = mouse.X + 12; if (left + 210 > width) left = Math.Max(0, mouse.X - 222);
            double top = Math.Max(0, Math.Min(Math.Max(0, height - 165), mouse.Y + 12));
            tip.Margin = new Thickness(left, top, 0, 0); tip.Visibility = Visibility.Visible;
        }

        private void Chart_MouseLeave(object sender, MouseEventArgs e)
        {
            PriceHoverCanvas.Children.Clear(); VolumeHoverCanvas.Children.Clear(); RsiHoverCanvas.Children.Clear();
            PriceHoverToolTip.Visibility = Visibility.Collapsed; VolumeHoverToolTip.Visibility = Visibility.Collapsed; RsiHoverToolTip.Visibility = Visibility.Collapsed;
        }

        private static double GetDataX(int i, double width, int count) => count <= 1 ? 0 : i * width / (count - 1.0);

        private static void AddLegend(Panel panel, string text, Brush brush)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 14, 0) };
            stack.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = brush, Margin = new Thickness(0, 0, 5, 0) });
            stack.Children.Add(new TextBlock { Text = text, Foreground = brush, FontSize = 10, VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(stack);
        }

        private void DrawYAxis(PriceAxisScale s)
        {
            double[] v = { s.Maximum, s.Maximum - s.Step, s.Maximum - 2 * s.Step, s.Maximum - 3 * s.Step, s.Minimum };
            YAxisMaximumText.Text = FormatAxisPrice(v[0], s.Step); YAxisUpperMiddleText.Text = FormatAxisPrice(v[1], s.Step);
            YAxisMiddleText.Text = FormatAxisPrice(v[2], s.Step); YAxisLowerMiddleText.Text = FormatAxisPrice(v[3], s.Step); YAxisMinimumText.Text = FormatAxisPrice(v[4], s.Step);
        }

        private static PriceAxisScale CreateNicePriceScale(double min, double max, int ticks)
        {
            if (max < min) (min, max) = (max, min);
            double range = max - min;
            if (range <= 0) { double e = Math.Max(1, Math.Abs(max) * .05); min -= e; max += e; range = max - min; }
            int intervals = Math.Max(1, ticks - 1); double step = NiceStep(range / intervals); double niceMin = Math.Floor(min / step) * step; double niceMax = niceMin + step * intervals;
            while (niceMax < max) { niceMax += step; niceMin = niceMax - step * intervals; }
            return new PriceAxisScale(niceMin, niceMax, step);
        }

        private static double NiceStep(double raw)
        {
            double exp = Math.Floor(Math.Log10(Math.Max(raw, 1e-12))), mag = Math.Pow(10, exp), n = raw / mag;
            return (n <= 1 ? 1 : n <= 2 ? 2 : n <= 2.5 ? 2.5 : n <= 5 ? 5 : 10) * mag;
        }

        private void DrawMonthlyAxis(IReadOnlyList<MarketCandle> candles, double width, double height, Canvas grid, Canvas labels)
        {
            grid.Children.Clear(); labels.Children.Clear(); bool years = candles[0].Date.Year != candles[^1].Date.Year;
            foreach (MonthTick tick in VisibleTicks(CreateMonthTicks(candles), width))
            {
                double x = GetDataX(tick.CandleIndex, width, candles.Count); AddVertical(grid, x, height);
                DateTime d = new(tick.Year, tick.Month, 1); string text = d.ToString(years ? "MMM yy" : "MMM", CultureInfo.CurrentCulture);
                var block = new TextBlock { Text = text, Foreground = FindBrush("TextMutedBrush"), FontSize = 9 };
                block.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); Canvas.SetLeft(block, Math.Clamp(x - block.DesiredSize.Width / 2, 0, Math.Max(0, width - block.DesiredSize.Width))); Canvas.SetTop(block, 7); labels.Children.Add(block);
            }
        }

        private void DrawMonthlyGridLinesOnly(IReadOnlyList<MarketCandle> candles, double width, double height, Canvas grid)
        {
            grid.Children.Clear(); foreach (MonthTick t in VisibleTicks(CreateMonthTicks(candles), width)) AddVertical(grid, GetDataX(t.CandleIndex, width, candles.Count), height);
        }

        private void AddVertical(Canvas c, double x, double h) => c.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = h, Stroke = FindBrush("BorderSubtleBrush"), StrokeThickness = 1, IsHitTestVisible = false });

        private static List<MonthTick> CreateMonthTicks(IReadOnlyList<MarketCandle> candles)
        {
            var result = new List<MonthTick>(); int? y = null, m = null;
            for (int i = 0; i < candles.Count; i++) { var d = candles[i].Date; if (y == d.Year && m == d.Month) continue; result.Add(new MonthTick(i, d.Year, d.Month)); y = d.Year; m = d.Month; }
            return result;
        }

        private static List<MonthTick> VisibleTicks(IReadOnlyList<MonthTick> ticks, double width)
        {
            if (ticks.Count == 0) return new(); int max = Math.Max(2, (int)Math.Floor(width / MinimumMonthLabelWidth)); int step = Math.Max(1, (int)Math.Ceiling(ticks.Count / (double)max));
            var result = ticks.Where((_, i) => i % step == 0).ToList(); if (result.Count == 0 || result[^1] != ticks[^1]) result.Add(ticks[^1]); return result;
        }

        private void ChartContainer_SizeChanged(object sender, SizeChangedEventArgs e) { if (_displayedCandles.Count >= 2) DrawCharts(_displayedCandles); }

        private void ClearChart()
        {
            PricePolyline.Points = new(); PricePolyline.Visibility = Visibility.Collapsed; EmptyChartState.Visibility = Visibility.Visible;
            MeanLinesCanvas.Children.Clear(); MeanLegendPanel.Children.Clear(); VolumeCanvas.Children.Clear(); EmptyVolumeState.Visibility = Visibility.Visible;
            RsiSeriesCanvas.Children.Clear(); RsiReferenceCanvas.Children.Clear(); RsiLegendPanel.Children.Clear(); EmptyRsiState.Visibility = Visibility.Visible;
            PriceHoverCanvas.Children.Clear(); VolumeHoverCanvas.Children.Clear(); RsiHoverCanvas.Children.Clear(); Chart_MouseLeave(this, null!);
            PriceMonthlyGridCanvas.Children.Clear(); PriceMonthlyLabelsCanvas.Children.Clear(); VolumeMonthlyGridCanvas.Children.Clear(); ClearVolumeAxis();
        }

        private void ClearVolumeAxis() { VolumeMaximumText.Text = ""; VolumeMiddleText.Text = ""; VolumeMinimumText.Text = "0"; }
        private static string GetMarketDisplayName(MarketDataStore s) => s.Markets.FirstOrDefault(m => string.Equals(m.Pair, s.Pair, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? s.Pair ?? "Market";
        private static string GetImportedIntervalDescription(int? i) => i switch { 1 => "1 Minute", 5 => "5 Minutes", 15 => "15 Minutes", 30 => "30 Minutes", 60 => "1 Hour", 240 => "4 Hours", 1440 => "Daily", 10080 => "Weekly", 21600 => "15 Days", null => "Imported", _ => $"{i} Minutes" };
        private static string FormatPrice(double v) => !double.IsFinite(v) ? "—" : Math.Abs(v) >= 1 ? v.ToString("N2", CultureInfo.CurrentCulture) : v.ToString("N6", CultureInfo.CurrentCulture);
        private static string FormatAxisPrice(double v, double step) => v.ToString(Math.Abs(step) >= 1 ? "N0" : Math.Abs(step) >= .01 ? "N2" : "N6", CultureInfo.CurrentCulture);
        private static string FormatVolume(double v) => Math.Abs(v) >= 1e9 ? (v / 1e9).ToString("0.##") + "B" : Math.Abs(v) >= 1e6 ? (v / 1e6).ToString("0.##") + "M" : Math.Abs(v) >= 1e3 ? (v / 1e3).ToString("0.##") + "K" : v.ToString("N0");
        private Brush FindBrush(string name) => (Brush)FindResource(name);

        private void ChartView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_storeSubscribed) { MarketDataStore.Instance.Changed -= StoreChanged; _storeSubscribed = false; }
        }

        private sealed record PriceAxisScale(double Minimum, double Maximum, double Step);
        private sealed record MonthTick(int CandleIndex, int Year, int Month);
    }
}

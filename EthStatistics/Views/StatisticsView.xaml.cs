using System;
using System.Collections.Generic;
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
    public partial class StatisticsView : UserControl
    {
        private readonly StatisticsService _statisticsService = new();

        public StatisticsView()
        {
            InitializeComponent();

            Loaded += StatisticsView_Loaded;
            Unloaded += StatisticsView_Unloaded;

            MarketDataStore.Instance.Changed +=
                MarketDataStore_Changed;
        }

        private void StatisticsView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            RefreshStatistics();
        }

        private void MarketDataStore_Changed(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Invoke(
                RefreshStatistics
            );
        }

        private void RefreshStatistics()
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            if (store.IsLoading)
            {
                MarketAnalysisText.Text =
                    $"Loading {store.Pair}...";

                MarketStatusText.Text =
                    "LOADING";

                MarketStatusIndicator.Fill =
                    FindBrush("WarningBrush");

                ClearStatistics();

                return;
            }

            if (!string.IsNullOrWhiteSpace(store.Error))
            {
                MarketAnalysisText.Text =
                    "Market data unavailable";

                MarketStatusText.Text =
                    "UNAVAILABLE";

                MarketStatusIndicator.Fill =
                    FindBrush("NegativeBrush");

                ClearStatistics();

                return;
            }

            if (
                store.Candles.Count < 2 ||
                store.Interval is null
            )
            {
                MarketAnalysisText.Text =
                    "Waiting for market data";

                MarketStatusText.Text =
                    "WAITING";

                MarketStatusIndicator.Fill =
                    FindBrush("TextMutedBrush");

                ClearStatistics();

                return;
            }

            try
            {
                StatisticsResult result =
                    _statisticsService.Calculate(
                        store.Candles,
                        store.Interval.Value,
                        store.ImportedReturns
                    );

                double[] returns =
                    GetReturns(store);

                MarketAnalysisText.Text =
                    $"{store.Pair} · {store.DataSource}";

                MarketStatusText.Text =
                    store.DataSource.StartsWith(
                        "Excel",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? "IMPORTED"
                        : "UPDATED";

                MarketStatusIndicator.Fill =
                    FindBrush("PositiveBrush");

                DistributionMarketText.Text =
                    store.Pair ?? "—";

                MeanValue.Text =
                    result.MeanPrice.ToString("N2");

                MinimumValue.Text =
                    result.MinimumPrice.ToString("N2");

                MaximumValue.Text =
                    result.MaximumPrice.ToString("N2");

                ObservationCountValue.Text =
                    result.ObservationCount.ToString();

                MeanReturnValue.Text =
                    result.MeanLogReturn.ToString("P4");

                VarianceValue.Text =
                    result.ReturnVariance.ToString("G6");

                StandardDeviationValue.Text =
                    result.ReturnStandardDeviation.ToString("P2");

                VolatilityValue.Text =
                    result.AnnualizedVolatility.ToString("P2");

                Quantile05Value.Text =
                    result.Quantile05.ToString("P2");

                Quantile10Value.Text =
                    result.Quantile10.ToString("P2");

                Quantile90Value.Text =
                    result.Quantile90.ToString("P2");

                Quantile95Value.Text =
                    result.Quantile95.ToString("P2");

                SkewnessValue.Text =
                    $"Skewness: {result.Skewness:N3}";

                KurtosisValue.Text =
                    result.ExcessKurtosis.ToString("N3");

                KurtosisDescriptionText.Text =
                    result.ExcessKurtosis switch
                    {
                        > 0.5 =>
                            "Heavier than normal tails",

                        < -0.5 =>
                            "Lighter than normal tails",

                        _ =>
                            "Near normal tail weight"
                    };

                DistributionShapeText.Text =
                    result.DistributionShape;

                DrawReturnDistribution(
                    returns
                );
            }
            catch (Exception error)
            {
                MarketAnalysisText.Text =
                    "Statistics unavailable";

                MarketStatusText.Text =
                    "ERROR";

                MarketStatusIndicator.Fill =
                    FindBrush("NegativeBrush");

                ClearStatistics();

                ToolTipService.SetToolTip(
                    MarketStatusText,
                    error.Message
                );
            }
        }

        private static double[] GetReturns(
            MarketDataStore store)
        {
            if (store.ImportedReturns.Count > 0)
            {
                return store.ImportedReturns
                    .Where(double.IsFinite)
                    .ToArray();
            }

            if (store.Candles.Count < 2)
            {
                return [];
            }

            var returns =
                new List<double>(
                    store.Candles.Count - 1
                );

            for (
                int index = 1;
                index < store.Candles.Count;
                index++
            )
            {
                double previousPrice =
                    store.Candles[index - 1].Close;

                double currentPrice =
                    store.Candles[index].Close;

                if (
                    previousPrice <= 0 ||
                    currentPrice <= 0
                )
                {
                    continue;
                }

                double logReturn =
                    Math.Log(
                        currentPrice /
                        previousPrice
                    );

                if (double.IsFinite(logReturn))
                {
                    returns.Add(logReturn);
                }
            }

            return returns.ToArray();
        }

        private void DrawReturnDistribution(
            IReadOnlyList<double> returns)
        {
            DistributionCanvas.Children.Clear();

            double width =
                DistributionCanvas.ActualWidth;

            double height =
                DistributionCanvas.ActualHeight;

            if (
                width <= 1 ||
                height <= 1
            )
            {
                return;
            }

            double[] validReturns =
                returns
                    .Where(double.IsFinite)
                    .ToArray();

            if (validReturns.Length < 2)
            {
                ShowEmptyDistribution();

                return;
            }

            double minimum =
                validReturns.Min();

            double maximum =
                validReturns.Max();

            double range =
                maximum - minimum;

            if (
                !double.IsFinite(range) ||
                range <= 0
            )
            {
                ShowEmptyDistribution();

                return;
            }

            int binCount =
                Math.Clamp(
                    (int)Math.Ceiling(
                        Math.Sqrt(
                            validReturns.Length
                        )
                    ),
                    5,
                    40
                );

            double binWidth =
                range /
                binCount;

            if (
                !double.IsFinite(binWidth) ||
                binWidth <= 0
            )
            {
                ShowEmptyDistribution();

                return;
            }

            int[] counts =
                new int[binCount];

            foreach (double value in validReturns)
            {
                int binIndex =
                    (int)(
                        (value - minimum) /
                        binWidth
                    );

                if (binIndex >= binCount)
                {
                    binIndex =
                        binCount - 1;
                }

                if (binIndex < 0)
                {
                    binIndex = 0;
                }

                counts[binIndex]++;
            }

            int maximumCount =
                counts.Max();

            if (maximumCount <= 0)
            {
                ShowEmptyDistribution();

                return;
            }

            double visualBarWidth =
                width /
                binCount;

            double maximumBarHeight =
                height * 0.92;

            for (
                int index = 0;
                index < binCount;
                index++
            )
            {
                int count =
                    counts[index];

                double heightRatio =
                    (double)count /
                    maximumCount;

                double barHeight =
                    heightRatio *
                    maximumBarHeight;

                double lowerBound =
                    minimum +
                    index * binWidth;

                double upperBound =
                    index == binCount - 1
                        ? maximum
                        : lowerBound + binWidth;

                double observationPercentage =
                    (double)count /
                    validReturns.Length;

                var tooltipContent =
                    new StackPanel();

                tooltipContent.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"{lowerBound:P3} to {upperBound:P3}",

                        FontWeight =
                            FontWeights.SemiBold
                    }
                );

                tooltipContent.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"Observations: {count}",

                        Margin =
                            new Thickness(0, 5, 0, 0)
                    }
                );

                tooltipContent.Children.Add(
                    new TextBlock
                    {
                        Text =
                            $"Share: {observationPercentage:P2}",

                        Margin =
                            new Thickness(0, 2, 0, 0)
                    }
                );

                var rectangle =
                    new Rectangle
                    {
                        Width =
                            Math.Max(
                                1,
                                visualBarWidth - 2
                            ),

                        Height =
                            Math.Max(
                                0,
                                barHeight
                            ),

                        Fill =
                            FindBrush("AccentBrush"),

                        Opacity =
                            0.72,

                        RadiusX = 2,
                        RadiusY = 2,

                        Cursor =
                            Cursors.Hand,

                        ToolTip =
                            new ToolTip
                            {
                                Content =
                                    tooltipContent
                            }
                    };

                rectangle.MouseEnter +=
                    (_, _) =>
                    {
                        rectangle.Opacity = 1.0;
                    };

                rectangle.MouseLeave +=
                    (_, _) =>
                    {
                        rectangle.Opacity = 0.72;
                    };

                Canvas.SetLeft(
                    rectangle,
                    index *
                    visualBarWidth
                );

                Canvas.SetTop(
                    rectangle,
                    height -
                    barHeight
                );

                DistributionCanvas.Children.Add(
                    rectangle
                );
            }

            MinimumReturnText.Text =
                minimum.ToString("P2");

            MaximumReturnText.Text =
                maximum.ToString("P2");

            EmptyDistributionState.Visibility =
                Visibility.Collapsed;
        }

        private void ShowEmptyDistribution()
        {
            DistributionCanvas.Children.Clear();

            MinimumReturnText.Text = "";
            MaximumReturnText.Text = "";

            EmptyDistributionState.Visibility =
                Visibility.Visible;
        }

        private void DistributionChart_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;

            if (
                store.IsLoading ||
                !string.IsNullOrWhiteSpace(
                    store.Error
                )
            )
            {
                return;
            }

            double[] returns =
                GetReturns(store);

            if (returns.Length > 1)
            {
                DrawReturnDistribution(
                    returns
                );
            }
            else
            {
                ShowEmptyDistribution();
            }
        }

        private void ClearStatistics()
        {
            MeanValue.Text = "—";
            MinimumValue.Text = "—";
            MaximumValue.Text = "—";
            ObservationCountValue.Text = "—";

            MeanReturnValue.Text = "—";
            VarianceValue.Text = "—";
            StandardDeviationValue.Text = "—";
            VolatilityValue.Text = "—";

            Quantile05Value.Text = "—";
            Quantile10Value.Text = "—";
            Quantile90Value.Text = "—";
            Quantile95Value.Text = "—";

            SkewnessValue.Text = "—";
            KurtosisValue.Text = "—";

            KurtosisDescriptionText.Text =
                "Tail weight";

            DistributionShapeText.Text =
                "—";

            DistributionMarketText.Text =
                "—";

            ShowEmptyDistribution();
        }

        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }

        private void StatisticsView_Unloaded(
            object sender,
            RoutedEventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                MarketDataStore_Changed;
        }
    }
}
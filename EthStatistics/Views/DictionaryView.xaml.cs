using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EthStatistics.Views
{
    public partial class DictionaryView : UserControl
    {
        // ==========================================
        // STATE
        // ==========================================

        private readonly List<DistributionPoint>
            _distributionPoints = new();

        private double
            _lastZ;

        private double
            _lastAlpha = 0.05;

        private string
            _lastAlternative = "TwoSided";


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public DictionaryView()
        {
            InitializeComponent();

            Loaded +=
                DictionaryView_Loaded;
        }


        // ==========================================
        // LOAD
        // ==========================================

        private void DictionaryView_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateDistributionInterface();

            DrawDistribution();

            DrawZTest();
        }


        // ==========================================
        // SEARCH
        // ==========================================

        private void SearchTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            string query =
                SearchTextBox
                    .Text
                    .Trim();

            ApplySearch(
                PopulationTopic,
                query,
                "population sample observation random variable sample size"
            );

            ApplySearch(
                MeanTopic,
                query,
                "mean arithmetic average central location"
            );

            ApplySearch(
                VarianceTopic,
                query,
                "variance standard deviation dispersion spread"
            );

            ApplySearch(
                ReturnsTopic,
                query,
                "financial simple log return price logarithm"
            );

            ApplySearch(
                ShapeTopic,
                query,
                "skewness kurtosis quantile tails asymmetry"
            );

            DistributionSection.Visibility =
                Matches(
                    query,
                    "distribution normal gaussian binomial poisson exponential probability density mass"
                )
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ApplySearch(
                HypothesisBasicsTopic,
                query,
                "hypothesis null alternative significance alpha type one type two error p-value"
            );

            ZTestSection.Visibility =
                Matches(
                    query,
                    "hypothesis z test mean known standard deviation p-value"
                )
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ApplySearch(
                TestingWorkflowTopic,
                query,
                "testing workflow assumptions test statistic critical region interpretation"
            );
        }


        private static void ApplySearch(
            FrameworkElement element,
            string query,
            string keywords)
        {
            element.Visibility =
                Matches(
                    query,
                    keywords
                )
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        private static bool Matches(
            string query,
            string keywords)
        {
            if (
                string.IsNullOrWhiteSpace(
                    query
                )
            )
            {
                return true;
            }

            return keywords.Contains(
                query,
                StringComparison.OrdinalIgnoreCase
            );
        }


        // ==========================================
        // SELECTED DISTRIBUTION
        // ==========================================

        private string SelectedDistribution
        {
            get
            {
                return
                    (
                        DistributionComboBox
                            .SelectedItem
                        as ComboBoxItem
                    )
                    ?.Tag
                    ?.ToString()
                    ??
                    "Normal";
            }
        }


        // ==========================================
        // DISTRIBUTION SELECTION
        // ==========================================

        private void DistributionComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            UpdateDistributionInterface();

            DrawDistribution();
        }


        private void DistributionParameterChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            DrawDistribution();
        }


        private void UpdateDistributionInterface()
        {
            string selected =
                SelectedDistribution;

            NormalParametersPanel.Visibility =
                selected == "Normal"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BinomialParametersPanel.Visibility =
                selected == "Binomial"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PoissonParametersPanel.Visibility =
                selected == "Poisson"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            ExponentialParametersPanel.Visibility =
                selected == "Exponential"
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            switch (selected)
            {
                case "Normal":

                    DistributionDescriptionText.Text =
                        "A continuous, symmetric distribution controlled by location μ and variance σ².";

                    DistributionFormulaText.Text =
                        "f(x) = [1 / √(2πσ²)] exp[-(x - μ)² / (2σ²)]";

                    break;


                case "Binomial":

                    DistributionDescriptionText.Text =
                        "The number of successes in n independent Bernoulli trials with success probability p.";

                    DistributionFormulaText.Text =
                        "P(X = k) = C(n,k) pᵏ (1-p)ⁿ⁻ᵏ";

                    break;


                case "Poisson":

                    DistributionDescriptionText.Text =
                        "A discrete count distribution for events occurring with average rate λ.";

                    DistributionFormulaText.Text =
                        "P(X = k) = exp(-λ) λᵏ / k!";

                    break;


                default:

                    DistributionDescriptionText.Text =
                        "A continuous waiting-time distribution with constant rate λ.";

                    DistributionFormulaText.Text =
                        "f(x) = λ exp(-λx), x ≥ 0";

                    break;
            }
        }


        // ==========================================
        // DISTRIBUTION RESIZE
        // ==========================================

        private void DistributionCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DrawDistribution();
        }


        // ==========================================
        // DRAW DISTRIBUTION
        // ==========================================

        private void DrawDistribution()
        {
            if (
                !IsLoaded
                ||
                DistributionCanvas.ActualWidth <= 1.0
                ||
                DistributionCanvas.ActualHeight <= 1.0
            )
            {
                return;
            }

            DistributionCanvas
                .Children
                .Clear();

            _distributionPoints
                .Clear();

            DistributionErrorText.Text =
                string.Empty;

            ClearDistributionHover();

            try
            {
                switch (SelectedDistribution)
                {
                    case "Normal":

                        DrawNormal();

                        break;


                    case "Binomial":

                        DrawBinomial();

                        break;


                    case "Poisson":

                        DrawPoisson();

                        break;


                    default:

                        DrawExponential();

                        break;
                }
            }
            catch (Exception error)
            {
                DistributionErrorText.Text =
                    error.Message;

                DistributionSummaryText.Text =
                    string.Empty;
            }
        }


        // ==========================================
        // NORMAL DISTRIBUTION
        // ==========================================

        private void DrawNormal()
        {
            double mean =
                ParseDouble(
                    NormalMeanTextBox.Text,
                    "Mean μ"
                );

            double variance =
                ParsePositive(
                    NormalVarianceTextBox.Text,
                    "Variance σ²"
                );

            double standardDeviation =
                Math.Sqrt(
                    variance
                );

            double minimum =
                mean
                -
                4.0
                *
                standardDeviation;

            double maximum =
                mean
                +
                4.0
                *
                standardDeviation;

            for (
                int index = 0;
                index <= 240;
                index++
            )
            {
                double x =
                    minimum
                    +
                    (
                        maximum
                        -
                        minimum
                    )
                    *
                    index
                    /
                    240.0;

                double difference =
                    x
                    -
                    mean;

                double exponent =
                    -
                    (
                        difference
                        *
                        difference
                    )
                    /
                    (
                        2.0
                        *
                        variance
                    );

                double density =
                    Math.Exp(
                        exponent
                    )
                    /
                    Math.Sqrt(
                        2.0
                        *
                        Math.PI
                        *
                        variance
                    );

                _distributionPoints.Add(
                    new DistributionPoint(
                        x,
                        density
                    )
                );
            }

            DistributionSummaryText.Text =
                $"E[X] = {mean:N4}   "
                +
                $"Var(X) = {variance:N4}   "
                +
                $"SD(X) = {standardDeviation:N4}";

            DrawContinuousCurve();
        }


        // ==========================================
        // BINOMIAL DISTRIBUTION
        // ==========================================

        private void DrawBinomial()
        {
            int trials =
                ParseInteger(
                    BinomialNTextBox.Text,
                    "Trials n",
                    1,
                    250
                );

            double probability =
                ParseProbability(
                    BinomialPTextBox.Text,
                    "Probability p"
                );

            for (
                int successes = 0;
                successes <= trials;
                successes++
            )
            {
                double mass =
                    BinomialPmf(
                        trials,
                        successes,
                        probability
                    );

                _distributionPoints.Add(
                    new DistributionPoint(
                        successes,
                        mass
                    )
                );
            }

            double expectedValue =
                trials
                *
                probability;

            double variance =
                trials
                *
                probability
                *
                (
                    1.0
                    -
                    probability
                );

            DistributionSummaryText.Text =
                $"E[X] = {expectedValue:N4}   "
                +
                $"Var(X) = {variance:N4}";

            DrawDiscreteBars();
        }


        // ==========================================
        // POISSON DISTRIBUTION
        // ==========================================

        private void DrawPoisson()
        {
            double lambda =
                ParsePositive(
                    PoissonLambdaTextBox.Text,
                    "Rate λ"
                );

            int maximum =
                Math.Max(
                    12,
                    (int)Math.Ceiling(
                        lambda
                        +
                        5.0
                        *
                        Math.Sqrt(
                            lambda
                        )
                    )
                );

            maximum =
                Math.Min(
                    maximum,
                    250
                );

            double probability =
                Math.Exp(
                    -lambda
                );

            for (
                int value = 0;
                value <= maximum;
                value++
            )
            {
                if (value > 0)
                {
                    probability *=
                        lambda
                        /
                        value;
                }

                _distributionPoints.Add(
                    new DistributionPoint(
                        value,
                        probability
                    )
                );
            }

            DistributionSummaryText.Text =
                $"E[X] = {lambda:N4}   "
                +
                $"Var(X) = {lambda:N4}";

            DrawDiscreteBars();
        }


        // ==========================================
        // EXPONENTIAL DISTRIBUTION
        // ==========================================

        private void DrawExponential()
        {
            double lambda =
                ParsePositive(
                    ExponentialLambdaTextBox.Text,
                    "Rate λ"
                );

            double maximum =
                6.0
                /
                lambda;

            for (
                int index = 0;
                index <= 240;
                index++
            )
            {
                double x =
                    maximum
                    *
                    index
                    /
                    240.0;

                double density =
                    lambda
                    *
                    Math.Exp(
                        -lambda
                        *
                        x
                    );

                _distributionPoints.Add(
                    new DistributionPoint(
                        x,
                        density
                    )
                );
            }

            double expectedValue =
                1.0
                /
                lambda;

            double variance =
                1.0
                /
                (
                    lambda
                    *
                    lambda
                );

            DistributionSummaryText.Text =
                $"E[X] = {expectedValue:N4}   "
                +
                $"Var(X) = {variance:N4}";

            DrawContinuousCurve();
        }


        // ==========================================
        // CONTINUOUS DISTRIBUTION CHART
        // ==========================================

        private void DrawContinuousCurve()
        {
            if (_distributionPoints.Count < 2)
            {
                return;
            }

            DrawDistributionAxes();

            var points =
                new PointCollection(
                    _distributionPoints.Count
                );

            foreach (
                DistributionPoint point
                in _distributionPoints
            )
            {
                Point screenPoint =
                    ConvertDistributionPointToScreen(
                        point
                    );

                points.Add(
                    screenPoint
                );
            }


            // Soft glow behind the main curve.

            var glowLine =
                new Polyline
                {
                    Points =
                        points,

                    Stroke =
                        FindBrush(
                            "AccentBrush"
                        ),

                    StrokeThickness =
                        8.0,

                    Opacity =
                        0.10,

                    StrokeLineJoin =
                        PenLineJoin.Round,

                    StrokeStartLineCap =
                        PenLineCap.Round,

                    StrokeEndLineCap =
                        PenLineCap.Round,

                    IsHitTestVisible =
                        false
                };

            DistributionCanvas
                .Children
                .Add(
                    glowLine
                );


            // Main probability density curve.

            var curve =
                new Polyline
                {
                    Points =
                        points,

                    Stroke =
                        FindBrush(
                            "AccentBrush"
                        ),

                    StrokeThickness =
                        2.4,

                    StrokeLineJoin =
                        PenLineJoin.Round,

                    StrokeStartLineCap =
                        PenLineCap.Round,

                    StrokeEndLineCap =
                        PenLineCap.Round,

                    IsHitTestVisible =
                        false
                };

            DistributionCanvas
                .Children
                .Add(
                    curve
                );
        }


        // ==========================================
        // DISCRETE DISTRIBUTION CHART
        // ==========================================

        private void DrawDiscreteBars()
        {
            if (_distributionPoints.Count == 0)
            {
                return;
            }

            DrawDistributionAxes();

            double width =
                DistributionCanvas
                    .ActualWidth;

            double height =
                DistributionCanvas
                    .ActualHeight;

            double maximumY =
                _distributionPoints.Max(
                    point =>
                        point.Y
                );

            if (maximumY <= 0.0)
            {
                return;
            }

            double plotWidth =
                Math.Max(
                    1.0,
                    width
                    -
                    50.0
                );

            double slotWidth =
                plotWidth
                /
                Math.Max(
                    1,
                    _distributionPoints.Count
                );

            foreach (
                DistributionPoint point
                in _distributionPoints
            )
            {
                Point screenPoint =
                    ConvertDistributionPointToScreen(
                        point
                    );

                double barHeight =
                    point.Y
                    /
                    maximumY
                    *
                    Math.Max(
                        1.0,
                        height
                        -
                        42.0
                    );

                double barWidth =
                    Math.Max(
                        2.0,
                        slotWidth
                        *
                        0.70
                    );

                var rectangle =
                    new Rectangle
                    {
                        Width =
                            barWidth,

                        Height =
                            Math.Max(
                                1.0,
                                barHeight
                            ),

                        Fill =
                            FindBrush(
                                "AccentBrush"
                            ),

                        Opacity =
                            0.78,

                        RadiusX =
                            2.0,

                        RadiusY =
                            2.0,

                        IsHitTestVisible =
                            false
                    };

                Canvas.SetLeft(
                    rectangle,
                    screenPoint.X
                    -
                    barWidth
                    /
                    2.0
                );

                Canvas.SetTop(
                    rectangle,
                    height
                    -
                    30.0
                    -
                    barHeight
                );

                DistributionCanvas
                    .Children
                    .Add(
                        rectangle
                    );
            }
        }


        // ==========================================
        // DISTRIBUTION AXES
        // ==========================================

        private void DrawDistributionAxes()
        {
            double width =
                DistributionCanvas
                    .ActualWidth;

            double height =
                DistributionCanvas
                    .ActualHeight;

            var horizontalAxis =
                new Line
                {
                    X1 =
                        42.0,

                    X2 =
                        Math.Max(
                            42.0,
                            width
                            -
                            8.0
                        ),

                    Y1 =
                        height
                        -
                        30.0,

                    Y2 =
                        height
                        -
                        30.0,

                    Stroke =
                        FindBrush(
                            "BorderSubtleBrush"
                        ),

                    StrokeThickness =
                        1.0,

                    IsHitTestVisible =
                        false
                };

            DistributionCanvas
                .Children
                .Add(
                    horizontalAxis
                );


            var verticalAxis =
                new Line
                {
                    X1 =
                        42.0,

                    X2 =
                        42.0,

                    Y1 =
                        8.0,

                    Y2 =
                        height
                        -
                        30.0,

                    Stroke =
                        FindBrush(
                            "BorderSubtleBrush"
                        ),

                    StrokeThickness =
                        1.0,

                    IsHitTestVisible =
                        false
                };

            DistributionCanvas
                .Children
                .Add(
                    verticalAxis
                );
        }


        // ==========================================
        // DISTRIBUTION HOVER
        // ==========================================

        private void DistributionChart_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (
                _distributionPoints.Count == 0
                ||
                DistributionInteractionArea.ActualWidth <= 1.0
                ||
                DistributionInteractionArea.ActualHeight <= 1.0
                ||
                DistributionCanvas.ActualWidth <= 1.0
            )
            {
                return;
            }

            Point mouse =
                e.GetPosition(
                    DistributionInteractionArea
                );

            int index =
                GetNearestDistributionIndex(
                    mouse.X
                );

            DistributionPoint point =
                _distributionPoints[index];

            Point chartPoint =
                ConvertDistributionPointToScreen(
                    point
                );

            DrawDistributionHover(
                chartPoint
            );

            UpdateDistributionHoverText(
                point
            );

            ShowDistributionHoverToolTip(
                mouse
            );
        }


        private void DistributionChart_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            ClearDistributionHover();
        }


        // ==========================================
        // NEAREST DISTRIBUTION POINT
        // ==========================================

        private int GetNearestDistributionIndex(
            double mouseX)
        {
            if (
                _distributionPoints.Count == 0
                ||
                DistributionCanvas.ActualWidth <= 1.0
            )
            {
                return 0;
            }

            double chartX =
                mouseX
                -
                DistributionCanvas
                    .Margin
                    .Left;

            double plotLeft =
                42.0;

            double plotRight =
                Math.Max(
                    plotLeft,
                    DistributionCanvas
                        .ActualWidth
                    -
                    8.0
                );

            chartX =
                Math.Clamp(
                    chartX,
                    plotLeft,
                    plotRight
                );


            // Discrete distributions should snap
            // to the nearest integer-valued point.

            if (
                SelectedDistribution
                is "Binomial"
                or "Poisson"
            )
            {
                double minimumX =
                    _distributionPoints
                        .First()
                        .X;

                double maximumX =
                    _distributionPoints
                        .Last()
                        .X;

                double ratio =
                    (
                        chartX
                        -
                        plotLeft
                    )
                    /
                    Math.Max(
                        1.0,
                        plotRight
                        -
                        plotLeft
                    );

                double targetX =
                    minimumX
                    +
                    ratio
                    *
                    (
                        maximumX
                        -
                        minimumX
                    );

                int nearestIndex =
                    0;

                double nearestDistance =
                    double.MaxValue;

                for (
                    int index = 0;
                    index < _distributionPoints.Count;
                    index++
                )
                {
                    double distance =
                        Math.Abs(
                            _distributionPoints[index].X
                            -
                            targetX
                        );

                    if (
                        distance
                        <
                        nearestDistance
                    )
                    {
                        nearestDistance =
                            distance;

                        nearestIndex =
                            index;
                    }
                }

                return nearestIndex;
            }


            // Continuous distributions have evenly
            // generated points.

            double continuousRatio =
                (
                    chartX
                    -
                    plotLeft
                )
                /
                Math.Max(
                    1.0,
                    plotRight
                    -
                    plotLeft
                );

            int continuousIndex =
                (int)Math.Round(
                    continuousRatio
                    *
                    (
                        _distributionPoints.Count
                        -
                        1
                    )
                );

            return Math.Clamp(
                continuousIndex,
                0,
                _distributionPoints.Count
                -
                1
            );
        }


        // ==========================================
        // DATA -> SCREEN COORDINATES
        // ==========================================

        private Point ConvertDistributionPointToScreen(
            DistributionPoint point)
        {
            double width =
                DistributionCanvas
                    .ActualWidth;

            double height =
                DistributionCanvas
                    .ActualHeight;

            double minimumX =
                _distributionPoints.Min(
                    item =>
                        item.X
                );

            double maximumX =
                _distributionPoints.Max(
                    item =>
                        item.X
                );

            double maximumY =
                _distributionPoints.Max(
                    item =>
                        item.Y
                );

            double xRange =
                maximumX
                -
                minimumX;

            if (xRange <= 0.0)
            {
                xRange =
                    1.0;
            }

            if (maximumY <= 0.0)
            {
                maximumY =
                    1.0;
            }

            double plotLeft =
                42.0;

            double plotRight =
                Math.Max(
                    plotLeft,
                    width
                    -
                    8.0
                );

            double plotTop =
                12.0;

            double plotBottom =
                Math.Max(
                    plotTop,
                    height
                    -
                    30.0
                );

            double x =
                plotLeft
                +
                (
                    point.X
                    -
                    minimumX
                )
                /
                xRange
                *
                (
                    plotRight
                    -
                    plotLeft
                );

            double y =
                plotTop
                +
                (
                    1.0
                    -
                    point.Y
                    /
                    maximumY
                )
                *
                (
                    plotBottom
                    -
                    plotTop
                );

            return new Point(
                x,
                y
            );
        }


        // ==========================================
        // DRAW HOVER CROSSHAIR
        // ==========================================

        private void DrawDistributionHover(
            Point chartPoint)
        {
            DistributionHoverCanvas
                .Children
                .Clear();

            double width =
                DistributionHoverCanvas
                    .ActualWidth;

            double height =
                DistributionHoverCanvas
                    .ActualHeight;


            // Vertical crosshair.

            var verticalLine =
                new Line
                {
                    X1 =
                        chartPoint.X,

                    X2 =
                        chartPoint.X,

                    Y1 =
                        0.0,

                    Y2 =
                        height,

                    Stroke =
                        FindBrush(
                            "AccentBrush"
                        ),

                    StrokeThickness =
                        1.0,

                    StrokeDashArray =
                        new DoubleCollection
                        {
                            3.0,
                            3.0
                        },

                    Opacity =
                        0.75,

                    IsHitTestVisible =
                        false
                };

            DistributionHoverCanvas
                .Children
                .Add(
                    verticalLine
                );


            // Horizontal crosshair.

            var horizontalLine =
                new Line
                {
                    X1 =
                        42.0,

                    X2 =
                        width,

                    Y1 =
                        chartPoint.Y,

                    Y2 =
                        chartPoint.Y,

                    Stroke =
                        FindBrush(
                            "AccentBrush"
                        ),

                    StrokeThickness =
                        1.0,

                    StrokeDashArray =
                        new DoubleCollection
                        {
                            3.0,
                            3.0
                        },

                    Opacity =
                        0.28,

                    IsHitTestVisible =
                        false
                };

            DistributionHoverCanvas
                .Children
                .Add(
                    horizontalLine
                );


            // Outer marker.

            var markerOuter =
                new Ellipse
                {
                    Width =
                        13.0,

                    Height =
                        13.0,

                    Fill =
                        FindBrush(
                            "AppBackgroundBrush"
                        ),

                    Stroke =
                        FindBrush(
                            "AccentBrush"
                        ),

                    StrokeThickness =
                        2.0,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                markerOuter,
                chartPoint.X
                -
                6.5
            );

            Canvas.SetTop(
                markerOuter,
                chartPoint.Y
                -
                6.5
            );

            DistributionHoverCanvas
                .Children
                .Add(
                    markerOuter
                );


            // Inner marker.

            var markerInner =
                new Ellipse
                {
                    Width =
                        5.0,

                    Height =
                        5.0,

                    Fill =
                        FindBrush(
                            "AccentBrush"
                        ),

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                markerInner,
                chartPoint.X
                -
                2.5
            );

            Canvas.SetTop(
                markerInner,
                chartPoint.Y
                -
                2.5
            );

            DistributionHoverCanvas
                .Children
                .Add(
                    markerInner
                );
        }


        // ==========================================
        // HOVER VALUES
        // ==========================================

        private void UpdateDistributionHoverText(
            DistributionPoint point)
        {
            bool discrete =
                SelectedDistribution
                is "Binomial"
                or
                "Poisson";

            DistributionHoverTitleText.Text =
                SelectedDistribution;

            if (discrete)
            {
                DistributionHoverXText.Text =
                    $"x = {point.X:N0}";

                DistributionHoverValueText.Text =
                    $"P(X = x) = {point.Y:N6}";

                DistributionHoverText.Text =
                    $"x = {point.X:N0}    "
                    +
                    $"probability = {point.Y:N6}";
            }
            else
            {
                DistributionHoverXText.Text =
                    $"x = {point.X:N4}";

                DistributionHoverValueText.Text =
                    $"f(x) = {point.Y:N6}";

                DistributionHoverText.Text =
                    $"x = {point.X:N4}    "
                    +
                    $"density = {point.Y:N6}";
            }
        }


        // ==========================================
        // FLOATING TOOLTIP
        // ==========================================

        private void ShowDistributionHoverToolTip(
            Point mouse)
        {
            const double estimatedWidth =
                185.0;

            const double estimatedHeight =
                90.0;

            const double offset =
                14.0;

            double availableWidth =
                DistributionInteractionArea
                    .ActualWidth;

            double availableHeight =
                DistributionInteractionArea
                    .ActualHeight;

            double left =
                mouse.X
                +
                offset;

            if (
                left
                +
                estimatedWidth
                >
                availableWidth
            )
            {
                left =
                    Math.Max(
                        0.0,
                        mouse.X
                        -
                        estimatedWidth
                        -
                        offset
                    );
            }

            double top =
                mouse.Y
                +
                offset;

            if (
                top
                +
                estimatedHeight
                >
                availableHeight
            )
            {
                top =
                    Math.Max(
                        0.0,
                        mouse.Y
                        -
                        estimatedHeight
                        -
                        offset
                    );
            }

            DistributionHoverToolTip.Margin =
                new Thickness(
                    left,
                    top,
                    0.0,
                    0.0
                );

            DistributionHoverToolTip.Visibility =
                Visibility.Visible;
        }


        private void ClearDistributionHover()
        {
            DistributionHoverCanvas
                .Children
                .Clear();

            DistributionHoverToolTip.Visibility =
                Visibility.Collapsed;

            DistributionHoverText.Text =
                "Move over the chart to inspect values";
        }


        // ==========================================
        // Z-TEST
        // ==========================================

        private void RunZTestButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                double sampleMean =
                    ParseDouble(
                        ZSampleMeanTextBox.Text,
                        "Sample mean"
                    );

                double nullMean =
                    ParseDouble(
                        ZNullMeanTextBox.Text,
                        "Null mean"
                    );

                double sigma =
                    ParsePositive(
                        ZSigmaTextBox.Text,
                        "Standard deviation σ"
                    );

                int sampleSize =
                    ParseInteger(
                        ZSampleSizeTextBox.Text,
                        "Sample size n",
                        1,
                        10_000_000
                    );

                double alpha =
                    ParseProbability(
                        ZAlphaTextBox.Text,
                        "Significance α"
                    );

                string alternative =
                    (
                        ZAlternativeComboBox
                            .SelectedItem
                        as ComboBoxItem
                    )
                    ?.Tag
                    ?.ToString()
                    ??
                    "TwoSided";

                double standardError =
                    sigma
                    /
                    Math.Sqrt(
                        sampleSize
                    );

                double z =
                    (
                        sampleMean
                        -
                        nullMean
                    )
                    /
                    standardError;

                double cdf =
                    StandardNormalCdf(
                        z
                    );

                double pValue =
                    alternative switch
                    {
                        "Right" =>
                            1.0
                            -
                            cdf,

                        "Left" =>
                            cdf,

                        _ =>
                            2.0
                            *
                            Math.Min(
                                cdf,
                                1.0
                                -
                                cdf
                            )
                    };

                pValue =
                    Math.Clamp(
                        pValue,
                        0.0,
                        1.0
                    );

                bool reject =
                    pValue
                    <=
                    alpha;

                ZTestResultText.Text =
                    $"z statistic: {z:N4}\n"
                    +
                    $"p-value: {pValue:N6}\n"
                    +
                    $"α: {alpha:N4}\n\n"
                    +
                    (
                        reject
                            ?
                            "Decision: Reject H₀. "
                            +
                            "The result is statistically significant at the selected α."
                            :
                            "Decision: Fail to reject H₀. "
                            +
                            "The result is not statistically significant at the selected α."
                    );

                _lastZ =
                    z;

                _lastAlpha =
                    alpha;

                _lastAlternative =
                    alternative;

                DrawZTest();
            }
            catch (Exception error)
            {
                ZTestResultText.Text =
                    error.Message;
            }
        }


        // ==========================================
        // Z-TEST RESIZE
        // ==========================================

        private void ZTestCanvas_SizeChanged(
            object sender,
            SizeChangedEventArgs e)
        {
            DrawZTest();
        }


        // ==========================================
        // DRAW Z-TEST
        // ==========================================

        private void DrawZTest()
        {
            if (
                !IsLoaded
                ||
                ZTestCanvas.ActualWidth <= 1.0
                ||
                ZTestCanvas.ActualHeight <= 1.0
            )
            {
                return;
            }

            ZTestCanvas
                .Children
                .Clear();

            double width =
                ZTestCanvas
                    .ActualWidth;

            double height =
                ZTestCanvas
                    .ActualHeight;

            double baseline =
                height
                -
                28.0;

            var curvePoints =
                new PointCollection();

            for (
                int index = 0;
                index <= 240;
                index++
            )
            {
                double z =
                    -4.0
                    +
                    8.0
                    *
                    index
                    /
                    240.0;

                double density =
                    Math.Exp(
                        -0.5
                        *
                        z
                        *
                        z
                    )
                    /
                    Math.Sqrt(
                        2.0
                        *
                        Math.PI
                    );

                double x =
                    36.0
                    +
                    index
                    /
                    240.0
                    *
                    (
                        width
                        -
                        50.0
                    );

                double y =
                    baseline
                    -
                    density
                    /
                    0.4
                    *
                    (
                        height
                        -
                        48.0
                    );

                curvePoints.Add(
                    new Point(
                        x,
                        y
                    )
                );
            }


            // X axis.

            ZTestCanvas
                .Children
                .Add(
                    new Line
                    {
                        X1 =
                            36.0,

                        X2 =
                            width
                            -
                            8.0,

                        Y1 =
                            baseline,

                        Y2 =
                            baseline,

                        Stroke =
                            FindBrush(
                                "BorderSubtleBrush"
                            ),

                        StrokeThickness =
                            1.0,

                        IsHitTestVisible =
                            false
                    }
                );


            // Soft glow.

            ZTestCanvas
                .Children
                .Add(
                    new Polyline
                    {
                        Points =
                            curvePoints,

                        Stroke =
                            FindBrush(
                                "AccentBrush"
                            ),

                        StrokeThickness =
                            7.0,

                        Opacity =
                            0.10,

                        IsHitTestVisible =
                            false
                    }
                );


            // Standard normal curve.

            ZTestCanvas
                .Children
                .Add(
                    new Polyline
                    {
                        Points =
                            curvePoints,

                        Stroke =
                            FindBrush(
                                "AccentBrush"
                            ),

                        StrokeThickness =
                            2.2,

                        StrokeLineJoin =
                            PenLineJoin.Round,

                        StrokeStartLineCap =
                            PenLineCap.Round,

                        StrokeEndLineCap =
                            PenLineCap.Round,

                        IsHitTestVisible =
                            false
                    }
                );


            // Observed z position.

            double zPosition =
                36.0
                +
                Math.Clamp(
                    (
                        _lastZ
                        +
                        4.0
                    )
                    /
                    8.0,
                    0.0,
                    1.0
                )
                *
                (
                    width
                    -
                    50.0
                );


            // Observed z line.

            ZTestCanvas
                .Children
                .Add(
                    new Line
                    {
                        X1 =
                            zPosition,

                        X2 =
                            zPosition,

                        Y1 =
                            10.0,

                        Y2 =
                            baseline,

                        Stroke =
                            FindBrush(
                                "NegativeBrush"
                            ),

                        StrokeThickness =
                            1.7,

                        StrokeDashArray =
                            new DoubleCollection
                            {
                                4.0,
                                3.0
                            },

                        IsHitTestVisible =
                            false
                    }
                );


            // Z marker.

            var marker =
                new Ellipse
                {
                    Width =
                        9.0,

                    Height =
                        9.0,

                    Fill =
                        FindBrush(
                            "NegativeBrush"
                        ),

                    Stroke =
                        FindBrush(
                            "AppBackgroundBrush"
                        ),

                    StrokeThickness =
                        1.5,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                marker,
                zPosition
                -
                4.5
            );

            Canvas.SetTop(
                marker,
                baseline
                -
                4.5
            );

            ZTestCanvas
                .Children
                .Add(
                    marker
                );


            // Z label.

            var label =
                new TextBlock
                {
                    Text =
                        $"z = {_lastZ:N2}",

                    Foreground =
                        FindBrush(
                            "NegativeBrush"
                        ),

                    FontSize =
                        10.0,

                    FontWeight =
                        FontWeights.SemiBold,

                    IsHitTestVisible =
                        false
                };

            Canvas.SetLeft(
                label,
                Math.Clamp(
                    zPosition
                    +
                    6.0,
                    0.0,
                    Math.Max(
                        0.0,
                        width
                        -
                        70.0
                    )
                )
            );

            Canvas.SetTop(
                label,
                8.0
            );

            ZTestCanvas
                .Children
                .Add(
                    label
                );
        }


        // ==========================================
        // BINOMIAL PMF
        // ==========================================

        private static double BinomialPmf(
            int trials,
            int successes,
            double probability)
        {
            if (probability == 0.0)
            {
                return successes == 0
                    ? 1.0
                    : 0.0;
            }

            if (probability == 1.0)
            {
                return successes == trials
                    ? 1.0
                    : 0.0;
            }

            double logCombination =
                LogFactorial(
                    trials
                )
                -
                LogFactorial(
                    successes
                )
                -
                LogFactorial(
                    trials
                    -
                    successes
                );

            double logProbability =
                logCombination
                +
                successes
                *
                Math.Log(
                    probability
                )
                +
                (
                    trials
                    -
                    successes
                )
                *
                Math.Log(
                    1.0
                    -
                    probability
                );

            return Math.Exp(
                logProbability
            );
        }


        private static double LogFactorial(
            int value)
        {
            double result =
                0.0;

            for (
                int current = 2;
                current <= value;
                current++
            )
            {
                result +=
                    Math.Log(
                        current
                    );
            }

            return result;
        }


        // ==========================================
        // STANDARD NORMAL CDF
        // ==========================================

        private static double StandardNormalCdf(
            double z)
        {
            double sign =
                z < 0.0
                    ? -1.0
                    : 1.0;

            double x =
                Math.Abs(
                    z
                )
                /
                Math.Sqrt(
                    2.0
                );

            double t =
                1.0
                /
                (
                    1.0
                    +
                    0.3275911
                    *
                    x
                );

            double errorFunction =
                1.0
                -
                (
                    (
                        (
                            (
                                (
                                    1.061405429
                                    *
                                    t
                                    -
                                    1.453152027
                                )
                                *
                                t
                                +
                                1.421413741
                            )
                            *
                            t
                            -
                            0.284496736
                        )
                        *
                        t
                        +
                        0.254829592
                    )
                    *
                    t
                    *
                    Math.Exp(
                        -x
                        *
                        x
                    )
                );

            return
                0.5
                *
                (
                    1.0
                    +
                    sign
                    *
                    errorFunction
                );
        }


        // ==========================================
        // PARSING
        // ==========================================

        private static double ParseDouble(
            string text,
            string name)
        {
            bool valid =
                double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.CurrentCulture,
                    out double value
                )
                ||
                double.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value
                );

            if (!valid)
            {
                throw new ArgumentException(
                    $"{name} must be a valid number."
                );
            }

            if (!double.IsFinite(value))
            {
                throw new ArgumentException(
                    $"{name} must be finite."
                );
            }

            return value;
        }


        private static double ParsePositive(
            string text,
            string name)
        {
            double value =
                ParseDouble(
                    text,
                    name
                );

            if (value <= 0.0)
            {
                throw new ArgumentException(
                    $"{name} must be greater than zero."
                );
            }

            return value;
        }


        private static double ParseProbability(
            string text,
            string name)
        {
            double value =
                ParseDouble(
                    text,
                    name
                );

            if (
                value <= 0.0
                ||
                value >= 1.0
            )
            {
                throw new ArgumentException(
                    $"{name} must be strictly between 0 and 1."
                );
            }

            return value;
        }


        private static int ParseInteger(
            string text,
            string name,
            int minimum,
            int maximum)
        {
            bool valid =
                int.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.CurrentCulture,
                    out int value
                );

            if (
                !valid
                ||
                value < minimum
                ||
                value > maximum
            )
            {
                throw new ArgumentException(
                    $"{name} must be an integer from {minimum} to {maximum}."
                );
            }

            return value;
        }


        // ==========================================
        // RESOURCES
        // ==========================================

        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }


        // ==========================================
        // INTERNAL TYPES
        // ==========================================

        private sealed record DistributionPoint(
            double X,
            double Y
        );
    }
}
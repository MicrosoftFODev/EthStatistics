namespace EthStatistics.Models
{
    public sealed class StatisticsResult
    {
        public double MeanPrice { get; init; }

        public double MinimumPrice { get; init; }

        public double MaximumPrice { get; init; }

        public int ObservationCount { get; init; }

        public double MeanLogReturn { get; init; }

        public double ReturnVariance { get; init; }

        public double ReturnStandardDeviation { get; init; }

        public double AnnualizedVolatility { get; init; }

        public double Quantile05 { get; init; }

        public double Quantile10 { get; init; }

        public double Quantile90 { get; init; }

        public double Quantile95 { get; init; }

        public double Skewness { get; init; }

        public double ExcessKurtosis { get; init; }

        public string DistributionShape { get; init; } = "";
    }
}
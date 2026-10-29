using System;
using System.Collections.Generic;
using System.Linq;
using EthStatistics.Models;

namespace EthStatistics.Services
{
    public sealed class StatisticsService
    {
        public StatisticsResult Calculate(
            IReadOnlyList<MarketCandle> candles,
            int intervalMinutes,
            IReadOnlyList<double>? suppliedReturns = null)
        {
            if (candles.Count < 2)
            {
                throw new InvalidOperationException(
                    "At least two observations are required."
                );
            }

            double[] closingPrices =
                candles
                    .Select(candle => candle.Close)
                    .Where(value =>
                        double.IsFinite(value) &&
                        value > 0)
                    .ToArray();

            if (closingPrices.Length < 2)
            {
                throw new InvalidOperationException(
                    "At least two valid closing prices are required."
                );
            }

            double[] returns =
                GetReturns(
                    closingPrices,
                    suppliedReturns
                );

            if (returns.Length < 4)
            {
                throw new InvalidOperationException(
                    "At least four valid returns are required."
                );
            }

            double meanPrice =
                NativeMethods.CalculateMean(
                    closingPrices
                );

            double meanReturn =
                NativeMethods.CalculateMean(
                    returns
                );

            double variance =
                NativeMethods.CalculateVariance(
                    returns
                );

            double standardDeviation =
                NativeMethods.CalculateStandardDeviation(
                    returns
                );

            double periodsPerYear =
                CalculatePeriodsPerYear(
                    intervalMinutes
                );

            double skewness =
                CalculateAdjustedSkewness(
                    returns
                );

            double excessKurtosis =
                CalculateAdjustedExcessKurtosis(
                    returns
                );

            return new StatisticsResult
            {
                MeanPrice =
                    meanPrice,

                MinimumPrice =
                    closingPrices.Min(),

                MaximumPrice =
                    closingPrices.Max(),

                ObservationCount =
                    closingPrices.Length,

                MeanLogReturn =
                    meanReturn,

                ReturnVariance =
                    variance,

                ReturnStandardDeviation =
                    standardDeviation,

                AnnualizedVolatility =
                    standardDeviation *
                    Math.Sqrt(periodsPerYear),

                Quantile05 =
                    CalculateQuantile(
                        returns,
                        0.05
                    ),

                Quantile10 =
                    CalculateQuantile(
                        returns,
                        0.10
                    ),

                Quantile90 =
                    CalculateQuantile(
                        returns,
                        0.90
                    ),

                Quantile95 =
                    CalculateQuantile(
                        returns,
                        0.95
                    ),

                Skewness =
                    skewness,

                ExcessKurtosis =
                    excessKurtosis,

                DistributionShape =
                    ClassifyDistribution(
                        skewness
                    )
            };
        }

        private static double[] GetReturns(
            IReadOnlyList<double> closingPrices,
            IReadOnlyList<double>? suppliedReturns)
        {
            if (
                suppliedReturns is not null &&
                suppliedReturns.Count > 0
            )
            {
                return suppliedReturns
                    .Where(double.IsFinite)
                    .ToArray();
            }

            var returns =
                new List<double>(
                    closingPrices.Count - 1
                );

            for (
                int index = 1;
                index < closingPrices.Count;
                index++)
            {
                double previous =
                    closingPrices[index - 1];

                double current =
                    closingPrices[index];

                if (
                    previous <= 0 ||
                    current <= 0
                )
                {
                    continue;
                }

                double value =
                    NativeMethods.LogReturn(
                        previous,
                        current
                    );

                if (double.IsFinite(value))
                {
                    returns.Add(value);
                }
            }

            return returns.ToArray();
        }

        private static double CalculateQuantile(
            IReadOnlyList<double> values,
            double probability)
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException(
                    "Quantiles require at least one value."
                );
            }

            if (
                probability < 0 ||
                probability > 1
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(probability)
                );
            }

            double[] sorted =
                values
                    .OrderBy(value => value)
                    .ToArray();

            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            double position =
                probability *
                (sorted.Length - 1);

            int lowerIndex =
                (int)Math.Floor(position);

            int upperIndex =
                (int)Math.Ceiling(position);

            if (lowerIndex == upperIndex)
            {
                return sorted[lowerIndex];
            }

            double fraction =
                position - lowerIndex;

            return
                sorted[lowerIndex] +
                fraction *
                (
                    sorted[upperIndex] -
                    sorted[lowerIndex]
                );
        }

        private static double CalculateAdjustedSkewness(
            IReadOnlyList<double> values)
        {
            int count =
                values.Count;

            if (count < 3)
            {
                return 0;
            }

            double mean =
                values.Average();

            double sumSquared = 0;
            double sumCubed = 0;

            foreach (double value in values)
            {
                double difference =
                    value - mean;

                double squared =
                    difference * difference;

                sumSquared +=
                    squared;

                sumCubed +=
                    squared * difference;
            }

            if (sumSquared <= 0)
            {
                return 0;
            }

            double sampleStandardDeviation =
                Math.Sqrt(
                    sumSquared /
                    (count - 1)
                );

            if (sampleStandardDeviation <= 0)
            {
                return 0;
            }

            double standardizedThirdMoment =
                sumCubed /
                Math.Pow(
                    sampleStandardDeviation,
                    3
                );

            return
                count /
                (
                    (double)(count - 1) *
                    (count - 2)
                )
                *
                standardizedThirdMoment;
        }

        private static double CalculateAdjustedExcessKurtosis(
            IReadOnlyList<double> values)
        {
            int count =
                values.Count;

            if (count < 4)
            {
                return 0;
            }

            double mean =
                values.Average();

            double sumSquared = 0;
            double sumFourth = 0;

            foreach (double value in values)
            {
                double difference =
                    value - mean;

                double squared =
                    difference * difference;

                sumSquared +=
                    squared;

                sumFourth +=
                    squared * squared;
            }

            if (sumSquared <= 0)
            {
                return 0;
            }

            double sampleVariance =
                sumSquared /
                (count - 1);

            if (sampleVariance <= 0)
            {
                return 0;
            }

            double firstTerm =
                (
                    (double)count *
                    (count + 1)
                )
                /
                (
                    (count - 1.0) *
                    (count - 2.0) *
                    (count - 3.0)
                )
                *
                (
                    sumFourth /
                    Math.Pow(
                        sampleVariance,
                        2
                    )
                );

            double correction =
                3.0 *
                Math.Pow(
                    count - 1.0,
                    2
                )
                /
                (
                    (count - 2.0) *
                    (count - 3.0)
                );

            return
                firstTerm -
                correction;
        }

        private static string ClassifyDistribution(
            double skewness)
        {
            const double symmetryThreshold =
                0.25;

            if (
                Math.Abs(skewness) <
                symmetryThreshold
            )
            {
                return
                    "Approximately symmetric";
            }

            return skewness < 0
                ? "Left-heavy tail"
                : "Right-heavy tail";
        }

        private static double CalculatePeriodsPerYear(
            int intervalMinutes)
        {
            if (intervalMinutes <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intervalMinutes)
                );
            }

            const double minutesPerDay =
                24.0 * 60.0;

            const double daysPerYear =
                365.0;

            return
                daysPerYear *
                minutesPerDay /
                intervalMinutes;
        }
    }
}
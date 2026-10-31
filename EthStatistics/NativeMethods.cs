using System;
using System.Runtime.InteropServices;

namespace EthStatistics
{
    internal static unsafe class NativeMethods
    {
        // ==========================================
        // DLL
        // ==========================================

        private const string QuantEngineDll =
            "QuantEngine.dll";


        // ==========================================
        // BASIC MATHEMATICS
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "Add",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double Add(
            double a,
            double b
        );


        // ==========================================
        // MEAN ESTIMATORS
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "Mean",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double Mean(
            double* values,
            int size
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "GeometricMean",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double GeometricMean(
            double* values,
            int size
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "HarmonicMean",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double HarmonicMean(
            double* values,
            int size
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "WeightedMean",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double WeightedMean(
            double* values,
            double* weights,
            int size
        );


        // ==========================================
        // DISPERSION
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "Variance",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double Variance(
            double* values,
            int size
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "StandardDeviation",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern double StandardDeviation(
            double* values,
            int size
        );


        // ==========================================
        // RETURNS
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "LogReturn",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        internal static extern double LogReturn(
            double previousPrice,
            double currentPrice
        );


        // ==========================================
        // RSI
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "WilderRSI",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void WilderRSI(
            double* prices,
            int size,
            int period,
            double* output
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "CutlerRSI",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void CutlerRSI(
            double* prices,
            int size,
            int period,
            double* output
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "ExponentialRSI",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void ExponentialRSI(
            double* prices,
            int size,
            int period,
            double* output
        );


        [DllImport(
            QuantEngineDll,
            EntryPoint = "StochasticRSI",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void StochasticRSI(
            double* prices,
            int size,
            int rsiPeriod,
            int stochasticPeriod,
            double* output
        );


        // ==========================================
        // PRICE STREAK
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "PriceStreak",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void PriceStreak(
            double* prices,
            int size,
            double* output
        );


        // ==========================================
        // PERCENT RANK
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "PercentRank",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void PercentRank(
            double* values,
            int size,
            int period,
            double* output
        );


        // ==========================================
        // CONNORS RSI
        // ==========================================

        [DllImport(
            QuantEngineDll,
            EntryPoint = "ConnorsRSI",
            CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true
        )]
        private static extern void ConnorsRSI(
            double* prices,
            int size,
            int priceRsiPeriod,
            int streakRsiPeriod,
            int rankPeriod,
            double* output
        );


        // ==========================================
        // ADD WRAPPER
        // ==========================================

        internal static double CalculateAdd(
            double a,
            double b)
        {
            return Add(
                a,
                b
            );
        }


        // ==========================================
        // ARITHMETIC MEAN WRAPPER
        // ==========================================

        internal static double CalculateMean(
            double[] values)
        {
            ValidateValues(
                values,
                1
            );

            fixed (double* valuesPointer = values)
            {
                return Mean(
                    valuesPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // GEOMETRIC MEAN WRAPPER
        // ==========================================

        internal static double CalculateGeometricMean(
            double[] values)
        {
            ValidatePositiveValues(
                values,
                1
            );

            fixed (double* valuesPointer = values)
            {
                return GeometricMean(
                    valuesPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // HARMONIC MEAN WRAPPER
        // ==========================================

        internal static double CalculateHarmonicMean(
            double[] values)
        {
            ValidatePositiveValues(
                values,
                1
            );

            fixed (double* valuesPointer = values)
            {
                return HarmonicMean(
                    valuesPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // WEIGHTED MEAN WRAPPER
        // ==========================================

        internal static double CalculateWeightedMean(
            double[] values,
            double[] weights)
        {
            ValidateValues(
                values,
                1
            );

            ValidateValues(
                weights,
                1
            );

            if (
                values.Length !=
                weights.Length
            )
            {
                throw new ArgumentException(
                    "Values and weights must have the same number of observations."
                );
            }

            bool hasPositiveWeight =
                false;

            for (
                int index = 0;
                index < weights.Length;
                index++
            )
            {
                if (weights[index] < 0.0)
                {
                    throw new ArgumentException(
                        "Weights cannot be negative.",
                        nameof(weights)
                    );
                }

                if (weights[index] > 0.0)
                {
                    hasPositiveWeight =
                        true;
                }
            }

            if (!hasPositiveWeight)
            {
                throw new ArgumentException(
                    "At least one weight must be positive.",
                    nameof(weights)
                );
            }

            fixed (double* valuesPointer = values)
            fixed (double* weightsPointer = weights)
            {
                return WeightedMean(
                    valuesPointer,
                    weightsPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // VARIANCE WRAPPER
        // ==========================================

        internal static double CalculateVariance(
            double[] values)
        {
            ValidateValues(
                values,
                2
            );

            fixed (double* valuesPointer = values)
            {
                return Variance(
                    valuesPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // STANDARD DEVIATION WRAPPER
        // ==========================================

        internal static double CalculateStandardDeviation(
            double[] values)
        {
            ValidateValues(
                values,
                2
            );

            fixed (double* valuesPointer = values)
            {
                return StandardDeviation(
                    valuesPointer,
                    values.Length
                );
            }
        }


        // ==========================================
        // WILDER RSI WRAPPER
        // ==========================================

        internal static double[] CalculateWilderRsi(
            double[] prices,
            int period)
        {
            ValidateRsiInput(
                prices,
                period
            );

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                WilderRSI(
                    pricesPointer,
                    prices.Length,
                    period,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // CUTLER RSI WRAPPER
        // ==========================================

        internal static double[] CalculateCutlerRsi(
            double[] prices,
            int period)
        {
            ValidateRsiInput(
                prices,
                period
            );

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                CutlerRSI(
                    pricesPointer,
                    prices.Length,
                    period,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // EXPONENTIAL RSI WRAPPER
        // ==========================================

        internal static double[] CalculateExponentialRsi(
            double[] prices,
            int period)
        {
            ValidateRsiInput(
                prices,
                period
            );

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                ExponentialRSI(
                    pricesPointer,
                    prices.Length,
                    period,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // STOCHASTIC RSI WRAPPER
        // ==========================================

        internal static double[] CalculateStochasticRsi(
            double[] prices,
            int rsiPeriod,
            int stochasticPeriod)
        {
            ValidateRsiInput(
                prices,
                rsiPeriod
            );

            if (stochasticPeriod <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stochasticPeriod),
                    "Stochastic period must be greater than zero."
                );
            }

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                StochasticRSI(
                    pricesPointer,
                    prices.Length,
                    rsiPeriod,
                    stochasticPeriod,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // PRICE STREAK WRAPPER
        // ==========================================

        internal static double[] CalculatePriceStreak(
            double[] prices)
        {
            ValidatePositiveValues(
                prices,
                1
            );

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                PriceStreak(
                    pricesPointer,
                    prices.Length,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // PERCENT RANK WRAPPER
        // ==========================================

        internal static double[] CalculatePercentRank(
            double[] values,
            int period)
        {
            ValidateValues(
                values,
                1
            );

            if (period <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(period),
                    "Period must be greater than zero."
                );
            }

            if (values.Length <= period)
            {
                throw new ArgumentException(
                    "The values array must contain more observations than the period.",
                    nameof(values)
                );
            }

            double[] output =
                CreateOutputArray(
                    values.Length
                );

            fixed (double* valuesPointer = values)
            fixed (double* outputPointer = output)
            {
                PercentRank(
                    valuesPointer,
                    values.Length,
                    period,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // CONNORS RSI WRAPPER
        // ==========================================

        internal static double[] CalculateConnorsRsi(
            double[] prices,
            int priceRsiPeriod,
            int streakRsiPeriod,
            int rankPeriod)
        {
            ValidatePositiveValues(
                prices,
                1
            );

            if (priceRsiPeriod <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(priceRsiPeriod)
                );
            }

            if (streakRsiPeriod <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(streakRsiPeriod)
                );
            }

            if (rankPeriod <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rankPeriod)
                );
            }

            double[] output =
                CreateOutputArray(
                    prices.Length
                );

            fixed (double* pricesPointer = prices)
            fixed (double* outputPointer = output)
            {
                ConnorsRSI(
                    pricesPointer,
                    prices.Length,
                    priceRsiPeriod,
                    streakRsiPeriod,
                    rankPeriod,
                    outputPointer
                );
            }

            return output;
        }


        // ==========================================
        // VALIDATION
        // ==========================================

        private static void ValidateValues(
            double[] values,
            int minimumSize)
        {
            ArgumentNullException.ThrowIfNull(
                values
            );

            if (values.Length < minimumSize)
            {
                throw new ArgumentException(
                    $"At least {minimumSize} values are required.",
                    nameof(values)
                );
            }

            for (
                int index = 0;
                index < values.Length;
                index++
            )
            {
                if (!double.IsFinite(values[index]))
                {
                    throw new ArgumentException(
                        "All values must be finite.",
                        nameof(values)
                    );
                }
            }
        }


        private static void ValidatePositiveValues(
            double[] values,
            int minimumSize)
        {
            ValidateValues(
                values,
                minimumSize
            );

            for (
                int index = 0;
                index < values.Length;
                index++
            )
            {
                if (values[index] <= 0.0)
                {
                    throw new ArgumentException(
                        "All values must be greater than zero.",
                        nameof(values)
                    );
                }
            }
        }


        private static void ValidateRsiInput(
            double[] prices,
            int period)
        {
            ValidatePositiveValues(
                prices,
                1
            );

            if (period <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(period),
                    "RSI period must be greater than zero."
                );
            }

            if (prices.Length <= period)
            {
                throw new ArgumentException(
                    "The price array must contain more observations than the RSI period.",
                    nameof(prices)
                );
            }
        }


        // ==========================================
        // OUTPUT
        // ==========================================

        private static double[] CreateOutputArray(
            int size)
        {
            double[] output =
                new double[size];

            Array.Fill(
                output,
                double.NaN
            );

            return output;
        }
    }
}
using System;
using System.Runtime.InteropServices;

namespace EthStatistics
{
    internal static unsafe partial class NativeMethods
    {
        [LibraryImport("QuantEngine.dll")]
        private static partial double Mean(
            double* values,
            int size
        );

        [LibraryImport("QuantEngine.dll")]
        private static partial double Variance(
            double* values,
            int size
        );

        [LibraryImport("QuantEngine.dll")]
        private static partial double StandardDeviation(
            double* values,
            int size
        );

        [LibraryImport("QuantEngine.dll")]
        internal static partial double LogReturn(
            double previousPrice,
            double currentPrice
        );


        internal static double CalculateMean(
            double[] values)
        {
            ValidateValues(
                values,
                1
            );

            fixed (double* pointer = values)
            {
                return Mean(
                    pointer,
                    values.Length
                );
            }
        }


        internal static double CalculateVariance(
            double[] values)
        {
            ValidateValues(
                values,
                2
            );

            fixed (double* pointer = values)
            {
                return Variance(
                    pointer,
                    values.Length
                );
            }
        }


        internal static double CalculateStandardDeviation(
            double[] values)
        {
            ValidateValues(
                values,
                2
            );

            fixed (double* pointer = values)
            {
                return StandardDeviation(
                    pointer,
                    values.Length
                );
            }
        }


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
        }
    }
}
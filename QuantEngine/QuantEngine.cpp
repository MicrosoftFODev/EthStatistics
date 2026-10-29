#include "pch.h"
#include "QuantEngine.h"

#include <cmath>
#include <limits>


// ==========================================
// MEAN
// ==========================================

double Mean(
    const double* values,
    int size)
{
    if (
        values == nullptr ||
        size <= 0
        )
    {
        return 0.0;
    }

    double sum =
        0.0;

    for (
        int i = 0;
        i < size;
        i++
        )
    {
        sum +=
            values[i];
    }

    return
        sum /
        static_cast<double>(size);
}


// ==========================================
// VARIANCE
// ==========================================

double Variance(
    const double* values,
    int size)
{
    if (
        values == nullptr ||
        size < 2
        )
    {
        return 0.0;
    }

    const double mean =
        Mean(
            values,
            size
        );

    double sumSquaredDifferences =
        0.0;

    for (
        int i = 0;
        i < size;
        i++
        )
    {
        const double difference =
            values[i] -
            mean;

        sumSquaredDifferences +=
            difference *
            difference;
    }

    return
        sumSquaredDifferences /
        static_cast<double>(
            size - 1
            );
}


// ==========================================
// STANDARD DEVIATION
// ==========================================

double StandardDeviation(
    const double* values,
    int size)
{
    if (
        values == nullptr ||
        size < 2
        )
    {
        return 0.0;
    }

    return std::sqrt(
        Variance(
            values,
            size
        )
    );
}


// ==========================================
// LOG RETURN
// ==========================================

double LogReturn(
    double previousPrice,
    double currentPrice)
{
    if (
        previousPrice <= 0.0 ||
        currentPrice <= 0.0
        )
    {
        return 0.0;
    }

    return std::log(
        currentPrice /
        previousPrice
    );
}


// ==========================================
// RSI INTERNAL HELPER
// ==========================================

static double CalculateRsiValue(
    double averageGain,
    double averageLoss)
{
    // No movement at all.

    if (
        averageGain <= 0.0 &&
        averageLoss <= 0.0
        )
    {
        return 50.0;
    }


    // Gains but no losses.

    if (averageLoss <= 0.0)
    {
        return 100.0;
    }


    // Losses but no gains.

    if (averageGain <= 0.0)
    {
        return 0.0;
    }


    const double relativeStrength =
        averageGain /
        averageLoss;


    return
        100.0 -
        (
            100.0 /
            (
                1.0 +
                relativeStrength
                )
            );
}


// ==========================================
// WILDER RSI
// ==========================================
//
// output must have space for "size" doubles.
//
// Values before the first available RSI
// are written as NaN.
//
// Example:
//
// double output[100];
//
// WilderRSI(
//     prices,
//     100,
//     14,
//     output
// );
//
// ==========================================

void WilderRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    // Initialize every output as unavailable.

    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        period <= 0 ||
        size <= period
        )
    {
        return;
    }


    double gainSum =
        0.0;

    double lossSum =
        0.0;


    // Initial period.

    for (
        int i = 1;
        i <= period;
        i++
        )
    {
        const double change =
            prices[i] -
            prices[i - 1];


        if (change > 0.0)
        {
            gainSum +=
                change;
        }
        else if (change < 0.0)
        {
            lossSum +=
                -change;
        }
    }


    double averageGain =
        gainSum /
        static_cast<double>(
            period
            );


    double averageLoss =
        lossSum /
        static_cast<double>(
            period
            );


    output[period] =
        CalculateRsiValue(
            averageGain,
            averageLoss
        );


    // Wilder recursive smoothing.

    for (
        int i = period + 1;
        i < size;
        i++
        )
    {
        const double change =
            prices[i] -
            prices[i - 1];


        const double gain =
            change > 0.0
            ? change
            : 0.0;


        const double loss =
            change < 0.0
            ? -change
            : 0.0;


        averageGain =
            (
                averageGain *
                static_cast<double>(
                    period - 1
                    )
                +
                gain
                )
            /
            static_cast<double>(
                period
                );


        averageLoss =
            (
                averageLoss *
                static_cast<double>(
                    period - 1
                    )
                +
                loss
                )
            /
            static_cast<double>(
                period
                );


        output[i] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );
    }
}


// ==========================================
// CUTLER / SIMPLE RSI
// ==========================================
//
// Uses rolling simple averages of gains
// and losses instead of Wilder smoothing.
//
// ==========================================

void CutlerRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        period <= 0 ||
        size <= period
        )
    {
        return;
    }


    double gainSum =
        0.0;

    double lossSum =
        0.0;


    // Build initial rolling window.

    for (
        int i = 1;
        i <= period;
        i++
        )
    {
        const double change =
            prices[i] -
            prices[i - 1];


        if (change > 0.0)
        {
            gainSum +=
                change;
        }
        else if (change < 0.0)
        {
            lossSum +=
                -change;
        }
    }


    output[period] =
        CalculateRsiValue(
            gainSum /
            static_cast<double>(
                period
                ),
            lossSum /
            static_cast<double>(
                period
                )
        );


    // Move the fixed-size rolling window.

    for (
        int i = period + 1;
        i < size;
        i++
        )
    {
        const double newChange =
            prices[i] -
            prices[i - 1];


        const double oldChange =
            prices[
                i - period
            ]
            -
            prices[
                i - period - 1
            ];


        // Add newest observation.

        if (newChange > 0.0)
        {
            gainSum +=
                newChange;
        }
        else if (newChange < 0.0)
        {
            lossSum +=
                -newChange;
        }


        // Remove oldest observation.

        if (oldChange > 0.0)
        {
            gainSum -=
                oldChange;
        }
        else if (oldChange < 0.0)
        {
            lossSum -=
                -oldChange;
        }


        const double averageGain =
            gainSum /
            static_cast<double>(
                period
                );


        const double averageLoss =
            lossSum /
            static_cast<double>(
                period
                );


        output[i] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );
    }
}


// ==========================================
// EXPONENTIAL RSI
// ==========================================
//
// Uses standard EMA:
//
// alpha = 2 / (period + 1)
//
// This differs from Wilder's smoothing.
//
// ==========================================

void ExponentialRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        period <= 0 ||
        size <= period
        )
    {
        return;
    }


    double gainSum =
        0.0;

    double lossSum =
        0.0;


    // Initial SMA seed.

    for (
        int i = 1;
        i <= period;
        i++
        )
    {
        const double change =
            prices[i] -
            prices[i - 1];


        if (change > 0.0)
        {
            gainSum +=
                change;
        }
        else if (change < 0.0)
        {
            lossSum +=
                -change;
        }
    }


    double averageGain =
        gainSum /
        static_cast<double>(
            period
            );


    double averageLoss =
        lossSum /
        static_cast<double>(
            period
            );


    output[period] =
        CalculateRsiValue(
            averageGain,
            averageLoss
        );


    const double alpha =
        2.0 /
        (
            static_cast<double>(
                period
                )
            +
            1.0
            );


    for (
        int i = period + 1;
        i < size;
        i++
        )
    {
        const double change =
            prices[i] -
            prices[i - 1];


        const double gain =
            change > 0.0
            ? change
            : 0.0;


        const double loss =
            change < 0.0
            ? -change
            : 0.0;


        averageGain =
            alpha *
            gain
            +
            (
                1.0 -
                alpha
                )
            *
            averageGain;


        averageLoss =
            alpha *
            loss
            +
            (
                1.0 -
                alpha
                )
            *
            averageLoss;


        output[i] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );
    }
}


// ==========================================
// STOCHASTIC RSI
// ==========================================
//
// First calculates Wilder RSI.
// Then:
//
// StochRSI =
// 100 * (RSI - min RSI)
//       / (max RSI - min RSI)
//
// over stochasticPeriod.
//
// ==========================================

void StochasticRSI(
    const double* prices,
    int size,
    int rsiPeriod,
    int stochasticPeriod,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        rsiPeriod <= 0 ||
        stochasticPeriod <= 0 ||
        size <= rsiPeriod
        )
    {
        return;
    }


    double* rsi =
        new double[size];


    WilderRSI(
        prices,
        size,
        rsiPeriod,
        rsi
    );


    for (
        int i =
        rsiPeriod +
        stochasticPeriod -
        1;
        i < size;
        i++
        )
    {
        double minimumRsi =
            std::numeric_limits<double>
            ::infinity();


        double maximumRsi =
            -std::numeric_limits<double>
            ::infinity();


        const int start =
            i -
            stochasticPeriod +
            1;


        bool validWindow =
            true;


        for (
            int j = start;
            j <= i;
            j++
            )
        {
            if (!std::isfinite(rsi[j]))
            {
                validWindow =
                    false;

                break;
            }


            if (rsi[j] < minimumRsi)
            {
                minimumRsi =
                    rsi[j];
            }


            if (rsi[j] > maximumRsi)
            {
                maximumRsi =
                    rsi[j];
            }
        }


        if (!validWindow)
        {
            continue;
        }


        const double range =
            maximumRsi -
            minimumRsi;


        if (range <= 0.0)
        {
            output[i] =
                0.0;
        }
        else
        {
            output[i] =
                100.0 *
                (
                    rsi[i] -
                    minimumRsi
                    )
                /
                range;
        }
    }


    delete[] rsi;
}


// ==========================================
// PRICE STREAK
// ==========================================
//
// Rising:
//
// 1, 2, 3, ...
//
// Falling:
//
// -1, -2, -3, ...
//
// No change:
//
// 0
//
// ==========================================

void PriceStreak(
    const double* prices,
    int size,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    output[0] =
        0.0;


    for (
        int i = 1;
        i < size;
        i++
        )
    {
        if (
            prices[i] >
            prices[i - 1]
            )
        {
            output[i] =
                output[i - 1] > 0.0
                ? output[i - 1] + 1.0
                : 1.0;
        }
        else if (
            prices[i] <
            prices[i - 1]
            )
        {
            output[i] =
                output[i - 1] < 0.0
                ? output[i - 1] - 1.0
                : -1.0;
        }
        else
        {
            output[i] =
                0.0;
        }
    }
}


// ==========================================
// PERCENT RANK
// ==========================================

void PercentRank(
    const double* values,
    int size,
    int period,
    double* output)
{
    if (
        values == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        period <= 0 ||
        size <= period
        )
    {
        return;
    }


    for (
        int i = period;
        i < size;
        i++
        )
    {
        if (!std::isfinite(values[i]))
        {
            continue;
        }


        int lowerCount =
            0;

        int equalCount =
            0;


        const int start =
            i -
            period;


        bool validWindow =
            true;


        for (
            int j = start;
            j < i;
            j++
            )
        {
            if (!std::isfinite(values[j]))
            {
                validWindow =
                    false;

                break;
            }


            if (values[j] < values[i])
            {
                lowerCount++;
            }
            else if (values[j] == values[i])
            {
                equalCount++;
            }
        }


        if (!validWindow)
        {
            continue;
        }


        output[i] =
            100.0 *
            (
                static_cast<double>(
                    lowerCount
                    )
                +
                0.5 *
                static_cast<double>(
                    equalCount
                    )
                )
            /
            static_cast<double>(
                period
                );
    }
}


// ==========================================
// CONNORS RSI
// ==========================================
//
// Components:
//
// 1. Price RSI
// 2. RSI of price streak
// 3. Percent rank of 1-day return
//
// Final:
//
// CRSI =
// (PriceRSI + StreakRSI + PercentRank) / 3
//
// ==========================================

void ConnorsRSI(
    const double* prices,
    int size,
    int priceRsiPeriod,
    int streakRsiPeriod,
    int rankPeriod,
    double* output)
{
    if (
        prices == nullptr ||
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        output[i] =
            std::numeric_limits<double>
            ::quiet_NaN();
    }


    if (
        priceRsiPeriod <= 0 ||
        streakRsiPeriod <= 0 ||
        rankPeriod <= 0
        )
    {
        return;
    }


    double* priceRsi =
        new double[size];


    double* streak =
        new double[size];


    double* streakRsi =
        new double[size];


    double* returns =
        new double[size];


    double* percentRank =
        new double[size];


    WilderRSI(
        prices,
        size,
        priceRsiPeriod,
        priceRsi
    );


    PriceStreak(
        prices,
        size,
        streak
    );


    WilderRSI(
        streak,
        size,
        streakRsiPeriod,
        streakRsi
    );


    returns[0] =
        std::numeric_limits<double>
        ::quiet_NaN();


    for (
        int i = 1;
        i < size;
        i++
        )
    {
        if (prices[i - 1] == 0.0)
        {
            returns[i] =
                std::numeric_limits<double>
                ::quiet_NaN();
        }
        else
        {
            returns[i] =
                100.0 *
                (
                    prices[i] /
                    prices[i - 1]
                    -
                    1.0
                    );
        }
    }


    PercentRank(
        returns,
        size,
        rankPeriod,
        percentRank
    );


    for (
        int i = 0;
        i < size;
        i++
        )
    {
        if (
            !std::isfinite(priceRsi[i]) ||
            !std::isfinite(streakRsi[i]) ||
            !std::isfinite(percentRank[i])
            )
        {
            continue;
        }


        output[i] =
            (
                priceRsi[i] +
                streakRsi[i] +
                percentRank[i]
                )
            /
            3.0;
    }


    delete[] priceRsi;
    delete[] streak;
    delete[] streakRsi;
    delete[] returns;
    delete[] percentRank;
}
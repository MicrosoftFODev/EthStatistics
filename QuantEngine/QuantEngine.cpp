#include "pch.h"
#include "QuantEngine.h"

#include <algorithm>
#include <cmath>
#include <limits>
#include <vector>

#ifdef min
#undef min
#endif

#ifdef max
#undef max
#endif
namespace
{
    // ==========================================
    // INTERNAL HELPERS
    // ==========================================

    double MissingValue()
    {
        return std::numeric_limits<double>
            ::quiet_NaN();
    }


    bool IsValidArray(
        const double* values,
        int size)
    {
        if (
            values == nullptr ||
            size <= 0
            )
        {
            return false;
        }


        for (
            int index = 0;
            index < size;
            index++
            )
        {
            if (!std::isfinite(values[index]))
            {
                return false;
            }
        }


        return true;
    }


    bool AreValidPrices(
        const double* prices,
        int size)
    {
        if (!IsValidArray(prices, size))
        {
            return false;
        }


        for (
            int index = 0;
            index < size;
            index++
            )
        {
            if (prices[index] <= 0.0)
            {
                return false;
            }
        }


        return true;
    }


    void InitializeOutput(
        double* output,
        int size)
    {
        if (
            output == nullptr ||
            size <= 0
            )
        {
            return;
        }


        for (
            int index = 0;
            index < size;
            index++
            )
        {
            output[index] =
                MissingValue();
        }
    }


    double CalculateRsiValue(
        double averageGain,
        double averageLoss)
    {
        if (
            averageGain <= 0.0 &&
            averageLoss <= 0.0
            )
        {
            return 50.0;
        }


        if (averageLoss <= 0.0)
        {
            return 100.0;
        }


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
}


// ==========================================
// ADDITION
// ==========================================

double Add(
    double a,
    double b)
{
    return a + b;
}


// ==========================================
// ARITHMETIC MEAN
// ==========================================
//
// x_bar = (1 / n) * sum(x_i)
//
// ==========================================

double Mean(
    const double* values,
    int size)
{
    if (!IsValidArray(values, size))
    {
        return MissingValue();
    }


    double sum =
        0.0;


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        sum +=
            values[index];
    }


    return
        sum /
        static_cast<double>(size);
}


// ==========================================
// GEOMETRIC MEAN
// ==========================================
//
// G = (product(x_i))^(1 / n)
//
// Numerically stable form:
//
// G = exp(
//     (1 / n) * sum(log(x_i))
// )
//
// ==========================================

double GeometricMean(
    const double* values,
    int size)
{
    if (!IsValidArray(values, size))
    {
        return MissingValue();
    }


    double sumOfLogs =
        0.0;


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        const double value =
            values[index];


        if (value <= 0.0)
        {
            return MissingValue();
        }


        sumOfLogs +=
            std::log(value);
    }


    const double meanLog =
        sumOfLogs /
        static_cast<double>(size);


    const double result =
        std::exp(meanLog);


    return
        std::isfinite(result)
        ? result
        : MissingValue();
}


// ==========================================
// HARMONIC MEAN
// ==========================================
//
// H = n / sum(1 / x_i)
//
// ==========================================

double HarmonicMean(
    const double* values,
    int size)
{
    if (!IsValidArray(values, size))
    {
        return MissingValue();
    }


    double reciprocalSum =
        0.0;


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        const double value =
            values[index];


        if (value <= 0.0)
        {
            return MissingValue();
        }


        reciprocalSum +=
            1.0 /
            value;
    }


    if (
        !std::isfinite(reciprocalSum) ||
        reciprocalSum <= 0.0
        )
    {
        return MissingValue();
    }


    const double result =
        static_cast<double>(size) /
        reciprocalSum;


    return
        std::isfinite(result)
        ? result
        : MissingValue();
}


// ==========================================
// WEIGHTED MEAN
// ==========================================
//
// x_w =
// sum(w_i * x_i) / sum(w_i)
//
// ==========================================

double WeightedMean(
    const double* values,
    const double* weights,
    int size)
{
    if (
        !IsValidArray(values, size) ||
        !IsValidArray(weights, size)
        )
    {
        return MissingValue();
    }


    double weightedSum =
        0.0;


    double totalWeight =
        0.0;


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        const double weight =
            weights[index];


        if (weight < 0.0)
        {
            return MissingValue();
        }


        weightedSum +=
            values[index] *
            weight;


        totalWeight +=
            weight;
    }


    if (
        !std::isfinite(totalWeight) ||
        totalWeight <= 0.0
        )
    {
        return MissingValue();
    }


    const double result =
        weightedSum /
        totalWeight;


    return
        std::isfinite(result)
        ? result
        : MissingValue();
}


// ==========================================
// SAMPLE VARIANCE
// ==========================================
//
// s^2 =
// sum((x_i - x_bar)^2) / (n - 1)
//
// ==========================================

double Variance(
    const double* values,
    int size)
{
    if (
        !IsValidArray(values, size) ||
        size < 2
        )
    {
        return MissingValue();
    }


    const double mean =
        Mean(
            values,
            size
        );


    if (!std::isfinite(mean))
    {
        return MissingValue();
    }


    double sumSquaredDifferences =
        0.0;


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        const double difference =
            values[index] -
            mean;


        sumSquaredDifferences +=
            difference *
            difference;
    }


    const double result =
        sumSquaredDifferences /
        static_cast<double>(
            size - 1
            );


    return
        std::isfinite(result)
        ? result
        : MissingValue();
}


// ==========================================
// SAMPLE STANDARD DEVIATION
// ==========================================

double StandardDeviation(
    const double* values,
    int size)
{
    const double variance =
        Variance(
            values,
            size
        );


    if (
        !std::isfinite(variance) ||
        variance < 0.0
        )
    {
        return MissingValue();
    }


    return std::sqrt(
        variance
    );
}


// ==========================================
// LOG RETURN
// ==========================================
//
// r_t = log(P_t / P_(t-1))
//
// ==========================================

double LogReturn(
    double previousPrice,
    double currentPrice)
{
    if (
        !std::isfinite(previousPrice) ||
        !std::isfinite(currentPrice) ||
        previousPrice <= 0.0 ||
        currentPrice <= 0.0
        )
    {
        return MissingValue();
    }


    return std::log(
        currentPrice /
        previousPrice
    );
}


// ==========================================
// WILDER RSI
// ==========================================

void WilderRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        !AreValidPrices(prices, size) ||
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


    for (
        int index = 1;
        index <= period;
        index++
        )
    {
        const double change =
            prices[index] -
            prices[index - 1];


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
        static_cast<double>(period);


    double averageLoss =
        lossSum /
        static_cast<double>(period);


    output[period] =
        CalculateRsiValue(
            averageGain,
            averageLoss
        );


    for (
        int index = period + 1;
        index < size;
        index++
        )
    {
        const double change =
            prices[index] -
            prices[index - 1];


        const double gain =
            std::max(
                change,
                0.0
            );


        const double loss =
            std::max(
                -change,
                0.0
            );


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
            static_cast<double>(period);


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
            static_cast<double>(period);


        output[index] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );
    }
}


// ==========================================
// CUTLER RSI
// ==========================================

void CutlerRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        !AreValidPrices(prices, size) ||
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


    for (
        int index = 1;
        index <= period;
        index++
        )
    {
        const double change =
            prices[index] -
            prices[index - 1];


        gainSum +=
            std::max(
                change,
                0.0
            );


        lossSum +=
            std::max(
                -change,
                0.0
            );
    }


    output[period] =
        CalculateRsiValue(
            gainSum /
            static_cast<double>(period),
            lossSum /
            static_cast<double>(period)
        );


    for (
        int index = period + 1;
        index < size;
        index++
        )
    {
        const double newChange =
            prices[index] -
            prices[index - 1];


        const double oldChange =
            prices[index - period] -
            prices[index - period - 1];


        gainSum +=
            std::max(
                newChange,
                0.0
            );


        lossSum +=
            std::max(
                -newChange,
                0.0
            );


        gainSum -=
            std::max(
                oldChange,
                0.0
            );


        lossSum -=
            std::max(
                -oldChange,
                0.0
            );


        // Avoid insignificant negative values
        // caused by floating-point subtraction.

        gainSum =
            std::max(
                gainSum,
                0.0
            );


        lossSum =
            std::max(
                lossSum,
                0.0
            );


        output[index] =
            CalculateRsiValue(
                gainSum /
                static_cast<double>(period),
                lossSum /
                static_cast<double>(period)
            );
    }
}


// ==========================================
// EXPONENTIAL RSI
// ==========================================

void ExponentialRSI(
    const double* prices,
    int size,
    int period,
    double* output)
{
    if (
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        !AreValidPrices(prices, size) ||
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


    for (
        int index = 1;
        index <= period;
        index++
        )
    {
        const double change =
            prices[index] -
            prices[index - 1];


        gainSum +=
            std::max(
                change,
                0.0
            );


        lossSum +=
            std::max(
                -change,
                0.0
            );
    }


    double averageGain =
        gainSum /
        static_cast<double>(period);


    double averageLoss =
        lossSum /
        static_cast<double>(period);


    output[period] =
        CalculateRsiValue(
            averageGain,
            averageLoss
        );


    const double alpha =
        2.0 /
        (
            static_cast<double>(period) +
            1.0
            );


    for (
        int index = period + 1;
        index < size;
        index++
        )
    {
        const double change =
            prices[index] -
            prices[index - 1];


        const double gain =
            std::max(
                change,
                0.0
            );


        const double loss =
            std::max(
                -change,
                0.0
            );


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


        output[index] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );
    }
}


// ==========================================
// STOCHASTIC RSI
// ==========================================

void StochasticRSI(
    const double* prices,
    int size,
    int rsiPeriod,
    int stochasticPeriod,
    double* output)
{
    if (
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        !AreValidPrices(prices, size) ||
        rsiPeriod <= 0 ||
        stochasticPeriod <= 0 ||
        size <= rsiPeriod
        )
    {
        return;
    }


    std::vector<double> rsi(
        static_cast<std::size_t>(size)
    );


    WilderRSI(
        prices,
        size,
        rsiPeriod,
        rsi.data()
    );


    const int firstIndex =
        rsiPeriod +
        stochasticPeriod -
        1;


    if (firstIndex >= size)
    {
        return;
    }


    for (
        int index = firstIndex;
        index < size;
        index++
        )
    {
        const int startIndex =
            index -
            stochasticPeriod +
            1;


        double minimumRsi =
            std::numeric_limits<double>
            ::infinity();


        double maximumRsi =
            -std::numeric_limits<double>
            ::infinity();


        bool validWindow =
            true;


        for (
            int windowIndex = startIndex;
            windowIndex <= index;
            windowIndex++
            )
        {
            const double value =
                rsi[
                    static_cast<std::size_t>(
                        windowIndex
                        )
                ];


            if (!std::isfinite(value))
            {
                validWindow =
                    false;

                break;
            }


            minimumRsi =
                std::min(
                    minimumRsi,
                    value
                );


            maximumRsi =
                std::max(
                    maximumRsi,
                    value
                );
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
            output[index] =
                0.0;
        }
        else
        {
            output[index] =
                100.0 *
                (
                    rsi[
                        static_cast<std::size_t>(
                            index
                            )
                    ]
                    -
                    minimumRsi
                    )
                /
                range;
        }
    }
}


// ==========================================
// PRICE STREAK
// ==========================================

void PriceStreak(
    const double* prices,
    int size,
    double* output)
{
    if (
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (!AreValidPrices(prices, size))
    {
        return;
    }


    output[0] =
        0.0;


    for (
        int index = 1;
        index < size;
        index++
        )
    {
        if (
            prices[index] >
            prices[index - 1]
            )
        {
            output[index] =
                output[index - 1] > 0.0
                ? output[index - 1] + 1.0
                : 1.0;
        }
        else if (
            prices[index] <
            prices[index - 1]
            )
        {
            output[index] =
                output[index - 1] < 0.0
                ? output[index - 1] - 1.0
                : -1.0;
        }
        else
        {
            output[index] =
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
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        values == nullptr ||
        period <= 0 ||
        size <= period
        )
    {
        return;
    }


    for (
        int index = period;
        index < size;
        index++
        )
    {
        const double currentValue =
            values[index];


        if (!std::isfinite(currentValue))
        {
            continue;
        }


        const int startIndex =
            index -
            period;


        int lowerCount =
            0;


        int equalCount =
            0;


        bool validWindow =
            true;


        for (
            int windowIndex = startIndex;
            windowIndex < index;
            windowIndex++
            )
        {
            const double historicalValue =
                values[windowIndex];


            if (!std::isfinite(historicalValue))
            {
                validWindow =
                    false;

                break;
            }


            if (
                historicalValue <
                currentValue
                )
            {
                lowerCount++;
            }
            else if (
                historicalValue ==
                currentValue
                )
            {
                equalCount++;
            }
        }


        if (!validWindow)
        {
            continue;
        }


        output[index] =
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
            static_cast<double>(period);
    }
}


// ==========================================
// CONNORS RSI
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
        output == nullptr ||
        size <= 0
        )
    {
        return;
    }


    InitializeOutput(
        output,
        size
    );


    if (
        !AreValidPrices(prices, size) ||
        priceRsiPeriod <= 0 ||
        streakRsiPeriod <= 0 ||
        rankPeriod <= 0
        )
    {
        return;
    }


    const std::size_t vectorSize =
        static_cast<std::size_t>(size);


    std::vector<double> priceRsi(
        vectorSize
    );


    std::vector<double> streak(
        vectorSize
    );


    std::vector<double> streakRsi(
        vectorSize
    );


    std::vector<double> returns(
        vectorSize,
        MissingValue()
    );


    std::vector<double> percentRank(
        vectorSize
    );


    WilderRSI(
        prices,
        size,
        priceRsiPeriod,
        priceRsi.data()
    );


    PriceStreak(
        prices,
        size,
        streak.data()
    );


    // Streaks may be negative, so use the RSI
    // function directly. WilderRSI validates
    // prices as positive, therefore we calculate
    // streak RSI locally here.

    if (size > streakRsiPeriod)
    {
        double gainSum =
            0.0;


        double lossSum =
            0.0;


        std::fill(
            streakRsi.begin(),
            streakRsi.end(),
            MissingValue()
        );


        for (
            int index = 1;
            index <= streakRsiPeriod;
            index++
            )
        {
            const double change =
                streak[
                    static_cast<std::size_t>(
                        index
                        )
                ]
                -
                streak[
                    static_cast<std::size_t>(
                        index - 1
                        )
                ];


            gainSum +=
                std::max(
                    change,
                    0.0
                );


            lossSum +=
                std::max(
                    -change,
                    0.0
                );
        }


        double averageGain =
            gainSum /
            static_cast<double>(
                streakRsiPeriod
                );


        double averageLoss =
            lossSum /
            static_cast<double>(
                streakRsiPeriod
                );


        streakRsi[
            static_cast<std::size_t>(
                streakRsiPeriod
                )
        ] =
            CalculateRsiValue(
                averageGain,
                averageLoss
            );


            for (
                int index = streakRsiPeriod + 1;
                index < size;
                index++
                )
            {
                const double change =
                    streak[
                        static_cast<std::size_t>(
                            index
                            )
                    ]
                    -
                    streak[
                        static_cast<std::size_t>(
                            index - 1
                            )
                    ];


                const double gain =
                    std::max(
                        change,
                        0.0
                    );


                const double loss =
                    std::max(
                        -change,
                        0.0
                    );


                averageGain =
                    (
                        averageGain *
                        static_cast<double>(
                            streakRsiPeriod - 1
                            )
                        +
                        gain
                        )
                    /
                    static_cast<double>(
                        streakRsiPeriod
                        );


                averageLoss =
                    (
                        averageLoss *
                        static_cast<double>(
                            streakRsiPeriod - 1
                            )
                        +
                        loss
                        )
                    /
                    static_cast<double>(
                        streakRsiPeriod
                        );


                streakRsi[
                    static_cast<std::size_t>(
                        index
                        )
                ] =
                    CalculateRsiValue(
                        averageGain,
                        averageLoss
                    );
            }
    }


    for (
        int index = 1;
        index < size;
        index++
        )
    {
        const double previousPrice =
            prices[index - 1];


        const double currentPrice =
            prices[index];


        if (
            previousPrice <= 0.0 ||
            currentPrice <= 0.0
            )
        {
            continue;
        }


        returns[
            static_cast<std::size_t>(
                index
                )
        ] =
            100.0 *
                (
                    currentPrice /
                    previousPrice
                    -
                    1.0
                    );
    }


    PercentRank(
        returns.data(),
        size,
        rankPeriod,
        percentRank.data()
    );


    for (
        int index = 0;
        index < size;
        index++
        )
    {
        const std::size_t position =
            static_cast<std::size_t>(
                index
                );


        if (
            !std::isfinite(priceRsi[position]) ||
            !std::isfinite(streakRsi[position]) ||
            !std::isfinite(percentRank[position])
            )
        {
            continue;
        }


        output[index] =
            (
                priceRsi[position]
                +
                streakRsi[position]
                +
                percentRank[position]
                )
            /
            3.0;
    }
}
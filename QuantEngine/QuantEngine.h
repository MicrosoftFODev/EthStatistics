#pragma once

#ifdef QUANTENGINE_EXPORTS
#define QUANTENGINE_API __declspec(dllexport)
#else
#define QUANTENGINE_API __declspec(dllimport)
#endif


extern "C"
{
    // ==========================================
    // BASIC MATHEMATICS
    // ==========================================

    QUANTENGINE_API double Add(
        double a,
        double b
    );


    // ==========================================
    // MEAN ESTIMATORS
    // ==========================================

    // Arithmetic mean:
    //
    // x_bar = (1 / n) * sum(x_i)

    QUANTENGINE_API double Mean(
        const double* values,
        int size
    );


    // Geometric mean:
    //
    // G = (product(x_i))^(1 / n)
    //
    // All values must be strictly positive.

    QUANTENGINE_API double GeometricMean(
        const double* values,
        int size
    );


    // Harmonic mean:
    //
    // H = n / sum(1 / x_i)
    //
    // All values must be strictly positive.

    QUANTENGINE_API double HarmonicMean(
        const double* values,
        int size
    );


    // Weighted mean:
    //
    // x_w = sum(w_i * x_i) / sum(w_i)
    //
    // Weights must be non-negative.
    // At least one weight must be positive.

    QUANTENGINE_API double WeightedMean(
        const double* values,
        const double* weights,
        int size
    );


    // ==========================================
    // DISPERSION
    // ==========================================

    // Sample variance:
    //
    // s^2 =
    // sum((x_i - x_bar)^2) / (n - 1)

    QUANTENGINE_API double Variance(
        const double* values,
        int size
    );


    // Sample standard deviation:
    //
    // s = sqrt(s^2)

    QUANTENGINE_API double StandardDeviation(
        const double* values,
        int size
    );


    // ==========================================
    // RETURNS
    // ==========================================

    // Log return:
    //
    // r_t = log(P_t / P_(t-1))

    QUANTENGINE_API double LogReturn(
        double previousPrice,
        double currentPrice
    );


    // ==========================================
    // RSI
    // ==========================================

    // Wilder RSI:
    //
    // Uses Wilder recursive smoothing.
    //
    // output must contain space for size values.

    QUANTENGINE_API void WilderRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    // Cutler RSI:
    //
    // Uses simple moving averages of gains
    // and losses.

    QUANTENGINE_API void CutlerRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    // Exponential RSI:
    //
    // Uses alpha = 2 / (period + 1).

    QUANTENGINE_API void ExponentialRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    // ==========================================
    // STOCHASTIC RSI
    // ==========================================

    // Stochastic RSI:
    //
    // 100 * (RSI - rolling minimum RSI)
    //     / (rolling maximum RSI
    //        - rolling minimum RSI)

    QUANTENGINE_API void StochasticRSI(
        const double* prices,
        int size,
        int rsiPeriod,
        int stochasticPeriod,
        double* output
    );


    // ==========================================
    // PRICE STREAK
    // ==========================================

    // Consecutive positive closes:
    //
    // 1, 2, 3, ...
    //
    // Consecutive negative closes:
    //
    // -1, -2, -3, ...

    QUANTENGINE_API void PriceStreak(
        const double* prices,
        int size,
        double* output
    );


    // ==========================================
    // PERCENT RANK
    // ==========================================

    QUANTENGINE_API void PercentRank(
        const double* values,
        int size,
        int period,
        double* output
    );


    // ==========================================
    // CONNORS RSI
    // ==========================================

    // Connors RSI combines:
    //
    // 1. Price RSI
    // 2. Price-streak RSI
    // 3. Percent rank of one-period returns

    QUANTENGINE_API void ConnorsRSI(
        const double* prices,
        int size,
        int priceRsiPeriod,
        int streakRsiPeriod,
        int rankPeriod,
        double* output
    );
}
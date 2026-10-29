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


    QUANTENGINE_API double Mean(
        const double* values,
        int size
    );


    QUANTENGINE_API double Variance(
        const double* values,
        int size
    );


    QUANTENGINE_API double StandardDeviation(
        const double* values,
        int size
    );


    // ==========================================
    // RETURNS
    // ==========================================

    QUANTENGINE_API double LogReturn(
        double previousPrice,
        double currentPrice
    );


    // ==========================================
    // RELATIVE STRENGTH INDEX
    // ==========================================

    QUANTENGINE_API void WilderRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    QUANTENGINE_API void CutlerRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    QUANTENGINE_API void ExponentialRSI(
        const double* prices,
        int size,
        int period,
        double* output
    );


    // ==========================================
    // STOCHASTIC RSI
    // ==========================================

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

    QUANTENGINE_API void ConnorsRSI(
        const double* prices,
        int size,
        int priceRsiPeriod,
        int streakRsiPeriod,
        int rankPeriod,
        double* output
    );
}
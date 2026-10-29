#pragma once

#ifdef QUANTENGINE_EXPORTS
#define QUANTENGINE_API __declspec(dllexport)
#else
#define QUANTENGINE_API __declspec(dllimport)
#endif

extern "C"
{
    QUANTENGINE_API double Add(double a, double b);

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

    QUANTENGINE_API double LogReturn(
        double previousPrice,
        double currentPrice
    );
}
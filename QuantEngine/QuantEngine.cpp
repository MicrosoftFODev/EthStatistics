#include "pch.h"
#include "QuantEngine.h"

#include <cmath>


double Add(double a, double b)
{
    return a + b;
}


double Mean(const double* values, int size)
{
    if (values == nullptr || size <= 0)
        return 0.0;

    double sum = 0.0;

    for (int i = 0; i < size; i++)
    {
        sum += values[i];
    }

    return sum / size;
}


double Variance(const double* values, int size)
{
    if (values == nullptr || size < 2)
        return 0.0;

    double mean = Mean(values, size);

    double sumSquaredDifferences = 0.0;

    for (int i = 0; i < size; i++)
    {
        double difference = values[i] - mean;

        sumSquaredDifferences += difference * difference;
    }

    return sumSquaredDifferences / (size - 1);
}


double StandardDeviation(const double* values, int size)
{
    if (values == nullptr || size < 2)
        return 0.0;

    return std::sqrt(
        Variance(values, size)
    );
}


double LogReturn(
    double previousPrice,
    double currentPrice
)
{
    if (previousPrice <= 0.0 || currentPrice <= 0.0)
        return 0.0;

    return std::log(
        currentPrice / previousPrice
    );
}
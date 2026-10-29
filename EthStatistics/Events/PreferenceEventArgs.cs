using System;

namespace EthStatistics
{
    public sealed class ChartPreferencesChangedEventArgs : EventArgs
    {
        public required string Pair { get; init; }

        public required int Interval { get; init; }

        public required int PeriodDays { get; init; }
    }


    public sealed class DataPreferencesChangedEventArgs : EventArgs
    {
        public required string QuoteCurrency { get; init; }

        public required int Interval { get; init; }
    }


    public sealed class GeneralPreferencesChangedEventArgs : EventArgs
    {
        public string? Language { get; init; }

        public string? Theme { get; init; }
    }
}
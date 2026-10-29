using System;

namespace EthStatistics
{
    public sealed class ChartPreferencesChangedEventArgs : EventArgs
    {
        public required string Pair { get; init; }

        public required int Interval { get; init; }

        public required int PeriodDays { get; init; }
    }
}
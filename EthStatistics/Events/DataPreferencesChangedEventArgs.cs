using System;

namespace EthStatistics
{
    public sealed class DataPreferencesChangedEventArgs : EventArgs
    {
        public required string Currency { get; init; }

        public required int Interval { get; init; }
    }
}
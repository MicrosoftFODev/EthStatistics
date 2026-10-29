using System;

namespace EthStatistics
{
    public sealed class DashboardMarketChangedEventArgs : EventArgs
    {
        public required string Pair { get; init; }
    }
}
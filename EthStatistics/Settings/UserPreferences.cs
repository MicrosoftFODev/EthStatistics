namespace EthStatistics.Settings
{
    public sealed class UserPreferences
    {
        public string? Language { get; set; }

        public string? Theme { get; set; }

        public string? QuoteCurrency { get; set; }

        public string? MarketPair { get; set; }

        public int? IntervalMinutes { get; set; }

        public int? PeriodDays { get; set; }
    }
}
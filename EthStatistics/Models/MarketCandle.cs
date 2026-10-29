namespace EthStatistics.Models
{
    public sealed class MarketCandle
    {
        public DateTimeOffset Date { get; set; }

        public double Open { get; set; }

        public double High { get; set; }

        public double Low { get; set; }

        public double Close { get; set; }

        public double Vwap { get; set; }

        public double Volume { get; set; }

        public int TradeCount { get; set; }
    }
}
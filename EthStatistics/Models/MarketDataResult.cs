using System.Text.Json.Serialization;

namespace EthStatistics.Models
{
    public sealed class MarketDataResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("pair")]
        public string Pair { get; set; } = "";

        [JsonPropertyName("interval")]
        public int Interval { get; set; }

        [JsonPropertyName("candles")]
        public List<MarketCandle> Candles { get; set; } = [];
    }
}
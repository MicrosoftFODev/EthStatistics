using System.Text.Json.Serialization;

namespace EthStatistics.Models
{
    public sealed class MarketMetadataResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("markets")]
        public List<KrakenMarket> Markets { get; set; } = [];

        [JsonPropertyName("quoteCurrencies")]
        public List<string> QuoteCurrencies { get; set; } = [];

        [JsonPropertyName("intervals")]
        public List<MarketInterval> Intervals { get; set; } = [];

        [JsonPropertyName("periods")]
        public List<MarketPeriod> Periods { get; set; } = [];
    }
}
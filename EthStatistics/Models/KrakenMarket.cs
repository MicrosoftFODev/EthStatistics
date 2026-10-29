using System.Text.Json.Serialization;

namespace EthStatistics.Models
{
    public sealed class KrakenMarket
    {
        [JsonPropertyName("pair")]
        public string Pair { get; set; } = "";

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = "";

        [JsonPropertyName("baseAsset")]
        public string BaseAsset { get; set; } = "";

        [JsonPropertyName("quoteAsset")]
        public string QuoteAsset { get; set; } = "";

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";
    }
}
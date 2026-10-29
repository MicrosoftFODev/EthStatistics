using System.Text.Json.Serialization;

namespace EthStatistics.Models
{
    public sealed class MarketInterval
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("minutes")]
        public int Minutes { get; set; }

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }
    }
}
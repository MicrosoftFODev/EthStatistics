using System.Text.Json.Serialization;

namespace EthStatistics.Models
{
    public sealed class MarketPeriod
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("days")]
        public int Days { get; set; }

        [JsonPropertyName("isDefault")]
        public bool IsDefault { get; set; }
    }
}
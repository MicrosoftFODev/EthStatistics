namespace EthStatistics.Models
{
    public sealed class KrakenMarket
    {
        public string Pair { get; init; } =
            "";

        public string DisplayName { get; init; } =
            "";

        public string BaseAsset { get; init; } =
            "";

        public string QuoteAsset { get; init; } =
            "";


        public override string ToString()
        {
            if (
                !string.IsNullOrWhiteSpace(
                    DisplayName
                )
            )
            {
                return DisplayName;
            }


            if (
                !string.IsNullOrWhiteSpace(
                    BaseAsset
                )
                &&
                !string.IsNullOrWhiteSpace(
                    QuoteAsset
                )
            )
            {
                return
                    $"{BaseAsset}/{QuoteAsset}";
            }


            return Pair;
        }
    }
}
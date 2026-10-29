namespace EthStatistics.Models
{
    public sealed class MarketPeriod
    {
        public string Name { get; init; } =
            "";

        public int Days { get; init; }

        public bool IsDefault { get; init; }


        public override string ToString()
        {
            if (
                !string.IsNullOrWhiteSpace(
                    Name
                )
            )
            {
                return Name;
            }


            return Days switch
            {
                30 =>
                    "30 Days",

                90 =>
                    "90 Days",

                180 =>
                    "180 Days",

                365 =>
                    "1 Year",

                720 =>
                    "Maximum",

                _ =>
                    $"{Days} Days"
            };
        }
    }
}
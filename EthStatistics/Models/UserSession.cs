namespace EthStatistics.Models
{
    public sealed class UserSession
    {
        public string AccountId { get; init; } = "";

        public string Username { get; init; } = "";

        public string DisplayName { get; init; } =
            "Guest";

        public bool IsAuthenticated { get; init; }


        public static UserSession Guest()
        {
            return new UserSession
            {
                AccountId = "",
                Username = "",
                DisplayName = "Guest",
                IsAuthenticated = false
            };
        }
    }
}
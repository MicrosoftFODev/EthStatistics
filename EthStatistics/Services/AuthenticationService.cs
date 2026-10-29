using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EthStatistics.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;

namespace EthStatistics.Services
{
    public sealed class AuthenticationService
    {
        private const string ClientId =
      "a5fb1e23-605f-4228-98a7-be4eb2a9bb7d";

        private const string Authority =
            "https://login.microsoftonline.com/common";

        private static readonly string[] Scopes =
        [
            "User.Read"
        ];


      

        private readonly IPublicClientApplication
            _application;


        public AuthenticationService()
        {
            var brokerOptions =
                new BrokerOptions(
                    BrokerOptions.OperatingSystems.Windows
                )
                {
                    Title = "EthStatistics"
                };

            _application =
                PublicClientApplicationBuilder
                    .Create(ClientId)
                    .WithAuthority(Authority)
                    .WithBroker(brokerOptions)
                    .Build();
        }


        // ==========================================
        // SILENT SIGN-IN
        // ==========================================

        public async Task<UserSession?>
            TrySignInSilentlyAsync()
        {
            IAccount? account =
                (
                    await _application
                        .GetAccountsAsync()
                )
                .FirstOrDefault();

            account ??=
                PublicClientApplication
                    .OperatingSystemAccount;

            try
            {
                AuthenticationResult result =
                    await _application
                        .AcquireTokenSilent(
                            Scopes,
                            account
                        )
                        .ExecuteAsync();

                return CreateSession(result);
            }
            catch (MsalUiRequiredException)
            {
                return null;
            }
            catch (MsalException)
            {
                return null;
            }
        }


        // ==========================================
        // INTERACTIVE WAM SIGN-IN
        // ==========================================

        public async Task<UserSession> SignInAsync(
            IntPtr parentWindowHandle)
        {
            IAccount? account =
                (
                    await _application
                        .GetAccountsAsync()
                )
                .FirstOrDefault();

            account ??=
                PublicClientApplication
                    .OperatingSystemAccount;

            try
            {
                AuthenticationResult silentResult =
                    await _application
                        .AcquireTokenSilent(
                            Scopes,
                            account
                        )
                        .ExecuteAsync();

                return CreateSession(
                    silentResult
                );
            }
            catch (MsalUiRequiredException)
            {
                // Interactive WAM sign-in is required.
            }

            AuthenticationResult interactiveResult =
                await _application
                    .AcquireTokenInteractive(
                        Scopes
                    )
                    .WithAccount(account)
                    .WithParentActivityOrWindow(
                        parentWindowHandle
                    )
                    .WithPrompt(
                        Prompt.SelectAccount
                    )
                    .ExecuteAsync();

            return CreateSession(
                interactiveResult
            );
        }


        // ==========================================
        // SIGN OUT
        // ==========================================

        public async Task SignOutAsync()
        {
            IEnumerable<IAccount> accounts =
                await _application
                    .GetAccountsAsync();

            foreach (IAccount account in accounts)
            {
                await _application.RemoveAsync(
                    account
                );
            }
        }


        // ==========================================
        // CREATE APPLICATION SESSION
        // ==========================================

        private static UserSession CreateSession(
            AuthenticationResult result)
        {
            string username =
                result.Account?.Username
                ?? GetClaim(
                    result.ClaimsPrincipal,
                    "preferred_username"
                )
                ?? GetClaim(
                    result.ClaimsPrincipal,
                    ClaimTypes.Email
                )
                ?? "";

            string displayName =
                GetClaim(
                    result.ClaimsPrincipal,
                    "name"
                )
                ?? GetClaim(
                    result.ClaimsPrincipal,
                    ClaimTypes.Name
                )
                ?? username;

            string accountId =
                result.Account?
                    .HomeAccountId?
                    .Identifier
                ?? GetClaim(
                    result.ClaimsPrincipal,
                    "sub"
                )
                ?? "";

            return new UserSession
            {
                AccountId =
                    accountId,

                Username =
                    username,

                DisplayName =
                    string.IsNullOrWhiteSpace(
                        displayName
                    )
                        ? "Microsoft User"
                        : displayName,

                IsAuthenticated =
                    true
            };
        }


        private static string? GetClaim(
            ClaimsPrincipal? claimsPrincipal,
            string claimType)
        {
            return claimsPrincipal?
                .Claims
                .FirstOrDefault(
                    claim =>
                        string.Equals(
                            claim.Type,
                            claimType,
                            StringComparison.OrdinalIgnoreCase
                        )
                )?
                .Value;
        }
    }
}
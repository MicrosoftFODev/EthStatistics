using System.Windows;
using Microsoft.Win32;

namespace EthStatistics.Services
{
    public sealed class ThemeService
    {
        private const string DarkThemePath =
            "Styles/DarkTheme.xaml";

        private const string LightThemePath =
            "Styles/LightTheme.xaml";


        public void ApplyTheme(string? requestedTheme)
        {
            string effectiveTheme =
                ResolveTheme(requestedTheme);

            string source =
                effectiveTheme == "Light"
                    ? LightThemePath
                    : DarkThemePath;

            ResourceDictionary newTheme =
                new()
                {
                    Source = new Uri(
                        source,
                        UriKind.Relative
                    )
                };

            ResourceDictionary resources =
                Application.Current.Resources;

            ResourceDictionary? oldTheme =
                resources.MergedDictionaries
                    .FirstOrDefault(
                        dictionary =>
                            IsThemeDictionary(dictionary)
                    );

            if (oldTheme is not null)
            {
                int index =
                    resources.MergedDictionaries.IndexOf(
                        oldTheme
                    );

                resources.MergedDictionaries[index] =
                    newTheme;
            }
            else
            {
                resources.MergedDictionaries.Insert(
                    0,
                    newTheme
                );
            }
        }


        private static string ResolveTheme(
            string? requestedTheme)
        {
            if (
                string.Equals(
                    requestedTheme,
                    "Light",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return "Light";
            }

            if (
                string.Equals(
                    requestedTheme,
                    "System",
                    StringComparison.OrdinalIgnoreCase)
            )
            {
                return IsWindowsLightTheme()
                    ? "Light"
                    : "Dark";
            }

            return "Dark";
        }


        private static bool IsThemeDictionary(
            ResourceDictionary dictionary)
        {
            string source =
                dictionary.Source?.OriginalString ?? "";

            return
                source.EndsWith(
                    "DarkTheme.xaml",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                source.EndsWith(
                    "LightTheme.xaml",
                    StringComparison.OrdinalIgnoreCase
                );
        }


        private static bool IsWindowsLightTheme()
        {
            const string registryPath =
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    registryPath
                );

            object? value =
                key?.GetValue(
                    "AppsUseLightTheme"
                );

            return value is int integer &&
                   integer != 0;
        }
    }
}
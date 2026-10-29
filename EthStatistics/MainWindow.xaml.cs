using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using EthStatistics.Models;
using EthStatistics.Services;
using EthStatistics.Settings;
using EthStatistics.Views;
using Microsoft.Identity.Client;

namespace EthStatistics
{
    public partial class MainWindow : Window
    {
        // ==========================================
        // CONSTANTS
        // ==========================================

        private const int DailyInterval =
            1440;

        private const int DefaultPeriodDays =
            365;


        // ==========================================
        // SERVICES
        // ==========================================

        private readonly PythonMarketDataService
            _marketDataService = new();

        private readonly UserPreferencesService
            _preferencesService = new();

        private readonly ThemeService
            _themeService = new();

        private readonly AuthenticationService
            _authenticationService = new();


        // ==========================================
        // STATE
        // ==========================================

        private UserPreferences
            _preferences = new();

        private UserSession
            _currentUser = UserSession.Guest();

        private CancellationTokenSource?
            _refreshCancellation;


        private string? CurrentPreferenceAccountId
        {
            get
            {
                return _currentUser.IsAuthenticated
                    ? _currentUser.AccountId
                    : null;
            }
        }


        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        public MainWindow()
        {
            InitializeComponent();

            ShowDashboard();

            Loaded +=
                MainWindow_Loaded;

            StateChanged +=
                MainWindow_StateChanged;

            SourceInitialized +=
                MainWindow_SourceInitialized;

            MarketDataStore.Instance.Changed +=
                MarketDataStore_Changed;
        }


        // ==========================================
        // FULL SCREEN
        // ==========================================

        private void MainWindow_SourceInitialized(
            object? sender,
            EventArgs e)
        {
            EnterFullScreen();
        }


        private void EnterFullScreen()
        {
            WindowState =
                WindowState.Normal;

            Left =
                0;

            Top =
                0;

            Width =
                SystemParameters.PrimaryScreenWidth;

            Height =
                SystemParameters.PrimaryScreenHeight;
        }


        // ==========================================
        // WINDOW CONTROLS
        // ==========================================

        private void MinimizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState.Minimized;
        }


        private void MaximizeButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            WindowState =
                WindowState ==
                WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
        }


        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }


        private void MainWindow_StateChanged(
            object? sender,
            EventArgs e)
        {
            MaximizeButton.Content =
                WindowState ==
                WindowState.Maximized
                    ? "❐"
                    : "□";
        }


        // ==========================================
        // STARTUP
        // ==========================================

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await TryAuthenticateAtStartupAsync();

            UpdateUserInterface();

            await LoadCurrentUserPreferencesAsync();

            await LoadMetadataAsync();
        }


        private async Task TryAuthenticateAtStartupAsync()
        {
            try
            {
                IntPtr windowHandle =
                    GetWindowHandle();

                _currentUser =
                    await _authenticationService
                        .SignInAsync(
                            windowHandle
                        );
            }
            catch (MsalClientException error)
                when (IsAuthenticationCancellation(error))
            {
                _currentUser =
                    UserSession.Guest();
            }
            catch
            {
                // Authentication is optional.
                // Continue in Guest mode.

                _currentUser =
                    UserSession.Guest();
            }
        }


        private IntPtr GetWindowHandle()
        {
            return new WindowInteropHelper(this)
                .Handle;
        }


        private static bool IsAuthenticationCancellation(
            MsalClientException error)
        {
            return
                string.Equals(
                    error.ErrorCode,
                    "authentication_canceled",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    error.ErrorCode,
                    "wam_account_picker_canceled",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                string.Equals(
                    error.ErrorCode,
                    "user_canceled",
                    StringComparison.OrdinalIgnoreCase
                );
        }


        // ==========================================
        // USER PREFERENCES
        // ==========================================

        private async Task LoadCurrentUserPreferencesAsync()
        {
            _preferences =
                await _preferencesService.LoadAsync(
                    CurrentPreferenceAccountId
                );

            // API data is always daily.

            _preferences.IntervalMinutes =
                DailyInterval;

            _themeService.ApplyTheme(
                _preferences.Theme
            );
        }


        private async Task SaveCurrentUserPreferencesAsync()
        {
            // Guarantee daily API data.

            _preferences.IntervalMinutes =
                DailyInterval;

            await _preferencesService.SaveAsync(
                _preferences,
                CurrentPreferenceAccountId
            );
        }


        // ==========================================
        // MARKET METADATA
        // ==========================================

        private async Task LoadMetadataAsync()
        {
            ShowLoadingStatus(
                "Loading available markets..."
            );

            try
            {
                MarketMetadataResult result =
                    await _marketDataService
                        .GetMetadataAsync();

                MarketDataStore.Instance.SetMetadata(
                    result
                );

                if (!result.Success)
                {
                    ShowErrorStatus(
                        result.Error ??
                        "Market metadata could not be loaded."
                    );

                    return;
                }


                // ----------------------------------
                // RESTORE MARKET
                // ----------------------------------

                KrakenMarket? selectedMarket =
                    null;

                if (
                    !string.IsNullOrWhiteSpace(
                        _preferences.MarketPair
                    )
                )
                {
                    selectedMarket =
                        result.Markets.FirstOrDefault(
                            market =>
                                string.Equals(
                                    market.Pair,
                                    _preferences.MarketPair,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        );
                }


                if (
                    selectedMarket is null &&
                    !string.IsNullOrWhiteSpace(
                        _preferences.QuoteCurrency
                    )
                )
                {
                    selectedMarket =
                        result.Markets.FirstOrDefault(
                            market =>
                                string.Equals(
                                    market.QuoteAsset,
                                    _preferences.QuoteCurrency,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        );
                }


                // Prefer ETH/USD for a brand-new profile.

                selectedMarket ??=
                    result.Markets.FirstOrDefault(
                        market =>
                            string.Equals(
                                market.Pair,
                                "ETHUSD",
                                StringComparison.OrdinalIgnoreCase
                            )
                    );


                selectedMarket ??=
                    result.Markets.FirstOrDefault();


                // ----------------------------------
                // DAILY API INTERVAL
                // ----------------------------------

                MarketInterval? selectedInterval =
                    result.Intervals.FirstOrDefault(
                        interval =>
                            interval.Minutes ==
                            DailyInterval
                    );


                if (selectedInterval is null)
                {
                    ShowErrorStatus(
                        "The API provider does not support daily candles."
                    );

                    return;
                }


                // ----------------------------------
                // RESTORE PERIOD
                // ----------------------------------

                MarketPeriod? selectedPeriod =
                    null;


                if (
                    _preferences.PeriodDays.HasValue
                )
                {
                    selectedPeriod =
                        result.Periods.FirstOrDefault(
                            period =>
                                period.Days ==
                                _preferences.PeriodDays.Value
                        );
                }


                selectedPeriod ??=
                    result.Periods.FirstOrDefault(
                        period =>
                            period.IsDefault
                    );


                selectedPeriod ??=
                    result.Periods.FirstOrDefault(
                        period =>
                            period.Days ==
                            DefaultPeriodDays
                    );


                selectedPeriod ??=
                    result.Periods.FirstOrDefault();


                // ----------------------------------
                // VALIDATE
                // ----------------------------------

                if (
                    selectedMarket is null ||
                    selectedPeriod is null
                )
                {
                    ShowErrorStatus(
                        "No usable market configuration is available."
                    );

                    return;
                }


                // ----------------------------------
                // UPDATE PREFERENCES
                // ----------------------------------

                _preferences.MarketPair =
                    selectedMarket.Pair;

                _preferences.QuoteCurrency =
                    selectedMarket.QuoteAsset;

                _preferences.IntervalMinutes =
                    DailyInterval;

                _preferences.PeriodDays =
                    selectedPeriod.Days;


                await SaveCurrentUserPreferencesAsync();


                // ----------------------------------
                // LOAD MARKET DATA
                // ----------------------------------

                await ApplySelectionAndRefreshAsync(
                    selectedMarket.Pair,
                    selectedPeriod.Days
                );
            }
            catch (Exception error)
            {
                MarketDataStore.Instance.SetError(
                    error.Message
                );

                ShowErrorStatus(
                    error.Message
                );
            }
        }


        // ==========================================
        // APPLY MARKET SELECTION
        // ==========================================

        private async Task ApplySelectionAndRefreshAsync(
            string pair,
            int periodDays)
        {
            MarketDataStore.Instance.SetSelection(
                pair,
                DailyInterval,
                periodDays
            );

            await RefreshMarketDataAsync();
        }


        // ==========================================
        // API DATA REFRESH
        // ==========================================

        private async Task RefreshMarketDataAsync()
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            if (
                string.IsNullOrWhiteSpace(
                    store.Pair
                )
            )
            {
                return;
            }


            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();


            _refreshCancellation =
                new CancellationTokenSource();


            string pair =
                store.Pair;


            MarketDataStore.Instance.SetLoading();


            ShowLoadingStatus(
                $"Loading {GetMarketDisplayName(pair)}..."
            );


            try
            {
                MarketDataResult result =
                    await _marketDataService
                        .GetMarketDataAsync(
                            pair,
                            DailyInterval,
                            _refreshCancellation.Token
                        );


                // Kraken returns recent daily candles.
                // Apply the selected period locally.

                List<MarketCandle> filteredCandles =
     FilterCandlesByPeriod(
         result.Candles,
         store.PeriodDays
     );

                result.Candles.Clear();
                result.Candles.AddRange(
                    filteredCandles
                );


                MarketDataStore.Instance.SetResult(
                    result
                );


                if (result.Success)
                {
                    ShowSuccessStatus(
                        result.Pair,
                        result.Candles.Count
                    );
                }
                else
                {
                    ShowErrorStatus(
                        result.Error ??
                        "Market data could not be loaded."
                    );
                }
            }
            catch (OperationCanceledException)
            {
                // A newer request replaced this one.
            }
            catch (Exception error)
            {
                MarketDataStore.Instance.SetError(
                    error.Message
                );

                ShowErrorStatus(
                    error.Message
                );
            }
        }


        private static System.Collections.Generic.List<MarketCandle>
            FilterCandlesByPeriod(
                System.Collections.Generic.IReadOnlyList<MarketCandle> candles,
                int periodDays)
        {
            if (
                candles.Count == 0 ||
                periodDays <= 0
            )
            {
                return candles.ToList();
            }


            DateTimeOffset latestDate =
                candles.Max(
                    candle =>
                        candle.Date
                );


            DateTimeOffset earliestDate =
                latestDate.AddDays(
                    -periodDays
                );


            return candles
                .Where(
                    candle =>
                        candle.Date >=
                        earliestDate
                )
                .OrderBy(
                    candle =>
                        candle.Date
                )
                .ToList();
        }


        // ==========================================
        // SWITCH EXCEL TO API
        // ==========================================

        private async void UseApiDataButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _refreshCancellation?.Cancel();


            MarketDataStore.Instance
                .ClearImportedData();


            ShowLoadingStatus(
                "Loading available markets..."
            );


            await LoadMetadataAsync();


            ShowDashboard();
        }


        // ==========================================
        // DATA STORE CHANGED
        // ==========================================

        private void MarketDataStore_Changed(
            object? sender,
            EventArgs e)
        {
            Dispatcher.Invoke(
                UpdateGlobalDataSource
            );
        }


        private void UpdateGlobalDataSource()
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            DataSourceText.Text =
                string.IsNullOrWhiteSpace(
                    store.DataSource
                )
                    ? "—"
                    : store.DataSource;


            bool isExcel =
                store.DataSource.StartsWith(
                    "Excel",
                    StringComparison.OrdinalIgnoreCase
                );


            UseApiDataButton.Visibility =
                isExcel
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        // ==========================================
        // DASHBOARD
        // ==========================================

        private void ShowDashboard()
        {
            MainContent.Content =
                new DashboardView();
        }


        private void DashboardButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowDashboard();
        }


        // ==========================================
        // CHART
        // ==========================================

        private void ChartButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowChart();
        }


        private void ShowChart()
        {
            var chartView =
                new ChartView(
                    _preferences.QuoteCurrency
                );

            chartView.ChartPreferencesChanged +=
                ChartPreferencesChanged;

            MainContent.Content =
                chartView;
        }


        private async void ChartPreferencesChanged(
            object? sender,
            ChartPreferencesChangedEventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            KrakenMarket? selectedMarket =
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.Pair,
                            e.Pair,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            if (selectedMarket is null)
            {
                ShowErrorStatus(
                    "The selected market is not available."
                );

                return;
            }


            int periodDays =
                e.PeriodDays > 0
                    ? e.PeriodDays
                    : DefaultPeriodDays;


            _preferences.MarketPair =
                selectedMarket.Pair;

            _preferences.QuoteCurrency =
                selectedMarket.QuoteAsset;

            _preferences.IntervalMinutes =
                DailyInterval;

            _preferences.PeriodDays =
                periodDays;


            await SaveCurrentUserPreferencesAsync();


            await ApplySelectionAndRefreshAsync(
                selectedMarket.Pair,
                periodDays
            );
        }


        // ==========================================
        // EXCEL IMPORT
        // ==========================================

        private void ImportExcelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var importView =
                new ImportExcelView();


            importView.ImportCompleted +=
                ImportExcelView_ImportCompleted;


            MainContent.Content =
                importView;
        }


        private void ImportExcelView_ImportCompleted(
            object? sender,
            EventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            DataStatusText.Text =
                $"{store.Pair} · " +
                $"{store.Candles.Count} imported observations";


            DataStatusIndicator.Fill =
                FindBrush(
                    "PositiveBrush"
                );


            ShowChart();
        }


        // ==========================================
        // STATISTICS
        // ==========================================

        private void StatisticsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content =
                new StatisticsView();
        }


        // ==========================================
        // FORECAST
        // ==========================================

        private void ForecastButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MainContent.Content =
                new ForecastView();
        }


        // ==========================================
        // SETTINGS
        // ==========================================

        private void SettingsMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            var settingsView =
                new SettingsView(
                    _preferences,
                    _preferencesService,
                    CurrentPreferenceAccountId
                );


            settingsView.DataPreferencesChanged +=
                SettingsView_DataPreferencesChanged;


            settingsView.GeneralPreferencesChanged +=
                SettingsView_GeneralPreferencesChanged;


            MainContent.Content =
                settingsView;
        }


        private async void SettingsView_DataPreferencesChanged(
      object? sender,
      DataPreferencesChangedEventArgs e)
        {
            MarketDataStore store =
                MarketDataStore.Instance;


            KrakenMarket? currentMarket =
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.Pair,
                            store.Pair,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            KrakenMarket? selectedMarket =
                null;



            if (currentMarket is not null)
            {
                selectedMarket =
                    store.Markets.FirstOrDefault(
                        market =>
                            string.Equals(
                                market.BaseAsset,
                                currentMarket.BaseAsset,
                                StringComparison.OrdinalIgnoreCase
                            )
                            &&
                            string.Equals(
                                market.QuoteAsset,
                                e.QuoteCurrency,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );
            }



            selectedMarket ??=
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.QuoteAsset,
                            e.QuoteCurrency,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            if (selectedMarket is null)
            {
                ShowErrorStatus(
                    "No market is available for the selected currency."
                );

                return;
            }


            int periodDays =
                store.PeriodDays > 0
                    ? store.PeriodDays
                    : DefaultPeriodDays;


            _preferences.QuoteCurrency =
                selectedMarket.QuoteAsset;

            _preferences.MarketPair =
                selectedMarket.Pair;

            _preferences.PeriodDays =
                periodDays;


            await SaveCurrentUserPreferencesAsync();


            await ApplySelectionAndRefreshAsync(
                selectedMarket.Pair,
                periodDays
            );
        }


        private async void SettingsView_GeneralPreferencesChanged(
            object? sender,
            GeneralPreferencesChangedEventArgs e)
        {
            _preferences.Language =
                e.Language;

            _preferences.Theme =
                e.Theme;


            _themeService.ApplyTheme(
                e.Theme
            );


            await SaveCurrentUserPreferencesAsync();
        }


        // ==========================================
        // USER SESSION
        // ==========================================

        private void UpdateUserInterface()
        {
            if (_currentUser.IsAuthenticated)
            {
                UserMenuItem.Header =
                    !string.IsNullOrWhiteSpace(
                        _currentUser.DisplayName
                    )
                        ? _currentUser.DisplayName
                        : !string.IsNullOrWhiteSpace(
                            _currentUser.Username
                        )
                            ? _currentUser.Username
                            : "Microsoft User";


                SignInMenuItem.Visibility =
                    Visibility.Collapsed;


                SignOutMenuItem.Visibility =
                    Visibility.Visible;
            }
            else
            {
                UserMenuItem.Header =
                    "Guest";


                SignInMenuItem.Visibility =
                    Visibility.Visible;


                SignOutMenuItem.Visibility =
                    Visibility.Collapsed;
            }
        }


        // ==========================================
        // SIGN IN
        // ==========================================

        private async void SignInMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                IntPtr windowHandle =
                    GetWindowHandle();


                _currentUser =
                    await _authenticationService
                        .SignInAsync(
                            windowHandle
                        );


                await LoadCurrentUserPreferencesAsync();


                UpdateUserInterface();


                MarketDataStore.Instance
                    .ClearImportedData();


                await LoadMetadataAsync();


                ShowDashboard();
            }
            catch (MsalClientException error)
                when (IsAuthenticationCancellation(error))
            {
                // User cancelled.
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Sign-in Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }


        // ==========================================
        // SIGN OUT
        // ==========================================

        private async void SignOutMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await _authenticationService
                    .SignOutAsync();


                _currentUser =
                    UserSession.Guest();


                await LoadCurrentUserPreferencesAsync();


                UpdateUserInterface();


                MarketDataStore.Instance
                    .ClearImportedData();


                await LoadMetadataAsync();


                ShowDashboard();
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Sign-out Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }


        // ==========================================
        // DISPLAY HELPERS
        // ==========================================

        private string GetMarketDisplayName(
            string pair)
        {
            KrakenMarket? market =
                MarketDataStore.Instance.Markets
                    .FirstOrDefault(
                        item =>
                            string.Equals(
                                item.Pair,
                                pair,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );


            return
                market?.DisplayName
                ?? pair;
        }


        // ==========================================
        // GLOBAL STATUS
        // ==========================================

        private void ShowLoadingStatus(
            string message)
        {
            DataStatusText.Text =
                message;


            DataStatusIndicator.Fill =
                FindBrush(
                    "WarningBrush"
                );
        }


        private void ShowSuccessStatus(
            string pair,
            int count)
        {
            DataStatusText.Text =
                $"{GetMarketDisplayName(pair)} · " +
                $"{count} daily observations";


            DataStatusIndicator.Fill =
                FindBrush(
                    "PositiveBrush"
                );
        }


        private void ShowErrorStatus(
            string error)
        {
            DataStatusText.Text =
                "Market data unavailable";


            DataStatusIndicator.Fill =
                FindBrush(
                    "NegativeBrush"
                );


            MessageBox.Show(
                error,
                "Market Data Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }


        private Brush FindBrush(
            string resourceName)
        {
            return (Brush)FindResource(
                resourceName
            );
        }


        // ==========================================
        // ABOUT
        // ==========================================

        private void AboutMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "Crypto Statistics\n" +
                "Quantitative cryptocurrency analysis.",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }


        // ==========================================
        // CLEANUP
        // ==========================================

        protected override void OnClosed(
            EventArgs e)
        {
            MarketDataStore.Instance.Changed -=
                MarketDataStore_Changed;


            _refreshCancellation?.Cancel();
            _refreshCancellation?.Dispose();


            base.OnClosed(e);
        }
    }
}
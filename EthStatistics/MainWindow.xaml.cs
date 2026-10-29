using System;
using System.Collections.Generic;
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

        private const string DefaultBaseAsset =
            "ETH";

        private const string DefaultQuoteCurrency =
            "USD";


        // ==========================================
        // SERVICES
        // ==========================================

        private readonly KrakenMarketDataService
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
        // WINDOW INITIALIZATION
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


        // ==========================================
        // AUTHENTICATION
        // ==========================================

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
                // Continue as Guest if startup
                // authentication is unavailable.

                _currentUser =
                    UserSession.Guest();
            }
        }


        private IntPtr GetWindowHandle()
        {
            return new WindowInteropHelper(
                this
            ).Handle;
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


            if (
                string.IsNullOrWhiteSpace(
                    _preferences.QuoteCurrency
                )
            )
            {
                _preferences.QuoteCurrency =
                    DefaultQuoteCurrency;
            }


            if (
                !_preferences.PeriodDays.HasValue
                ||
                _preferences.PeriodDays.Value <= 0
            )
            {
                _preferences.PeriodDays =
                    DefaultPeriodDays;
            }


            // Keep this assignment only while the
            // UserPreferences model still contains
            // IntervalMinutes.

            _preferences.IntervalMinutes =
                DailyInterval;


            _themeService.ApplyTheme(
                _preferences.Theme
            );
        }


        private async Task SaveCurrentUserPreferencesAsync()
        {
            // API analysis is always daily.
            // Keep this assignment only while the
            // preference property still exists.

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
                        result.Error
                        ??
                        "Market metadata could not be loaded."
                    );

                    return;
                }


                KrakenMarket? selectedMarket =
                    SelectInitialMarket(
                        result.Markets
                    );


                MarketPeriod? selectedPeriod =
                    SelectInitialPeriod(
                        result.Periods
                    );


                if (
                    selectedMarket is null
                    ||
                    selectedPeriod is null
                )
                {
                    ShowErrorStatus(
                        "No usable market configuration is available."
                    );

                    return;
                }


                _preferences.MarketPair =
                    selectedMarket.Pair;

                _preferences.QuoteCurrency =
                    selectedMarket.QuoteAsset;

                _preferences.PeriodDays =
                    selectedPeriod.Days;

                _preferences.IntervalMinutes =
                    DailyInterval;


                await SaveCurrentUserPreferencesAsync();


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


        private KrakenMarket? SelectInitialMarket(
            IReadOnlyList<KrakenMarket> markets)
        {
            if (markets.Count == 0)
            {
                return null;
            }


            string quoteCurrency =
                string.IsNullOrWhiteSpace(
                    _preferences.QuoteCurrency
                )
                    ? DefaultQuoteCurrency
                    : _preferences
                        .QuoteCurrency
                        .Trim()
                        .ToUpperInvariant();


            List<KrakenMarket> marketsForCurrency =
                markets
                    .Where(
                        market =>
                            string.Equals(
                                market.QuoteAsset,
                                quoteCurrency,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .ToList();


            // First restore the saved pair, but only
            // if it belongs to the selected currency.

            KrakenMarket? selectedMarket =
                marketsForCurrency.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.Pair,
                            _preferences.MarketPair,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            // For a new profile, prefer ETH with the
            // user's selected quote currency.

            selectedMarket ??=
                marketsForCurrency.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.BaseAsset,
                            DefaultBaseAsset,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            selectedMarket ??=
                marketsForCurrency.FirstOrDefault();


            // If the saved quote currency has no
            // usable pairs, fall back to ETH/USD.

            selectedMarket ??=
                markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.BaseAsset,
                            DefaultBaseAsset,
                            StringComparison.OrdinalIgnoreCase
                        )
                        &&
                        string.Equals(
                            market.QuoteAsset,
                            DefaultQuoteCurrency,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            selectedMarket ??=
                markets.FirstOrDefault();


            return selectedMarket;
        }


        private MarketPeriod? SelectInitialPeriod(
            IReadOnlyList<MarketPeriod> periods)
        {
            if (periods.Count == 0)
            {
                return null;
            }


            MarketPeriod? selectedPeriod =
                null;


            if (
                _preferences.PeriodDays.HasValue
            )
            {
                selectedPeriod =
                    periods.FirstOrDefault(
                        period =>
                            period.Days ==
                            _preferences.PeriodDays.Value
                    );
            }


            selectedPeriod ??=
                periods.FirstOrDefault(
                    period =>
                        period.IsDefault
                );


            selectedPeriod ??=
                periods.FirstOrDefault(
                    period =>
                        period.Days ==
                        DefaultPeriodDays
                );


            selectedPeriod ??=
                periods.FirstOrDefault();


            return selectedPeriod;
        }


        // ==========================================
        // APPLY MARKET SELECTION
        // ==========================================

        private async Task ApplySelectionAndRefreshAsync(
            string pair,
            int periodDays)
        {
            if (
                string.IsNullOrWhiteSpace(
                    pair
                )
            )
            {
                return;
            }


            int validPeriodDays =
                periodDays > 0
                    ? periodDays
                    : DefaultPeriodDays;


            MarketDataStore.Instance.SetSelection(
                pair.Trim().ToUpperInvariant(),
                DailyInterval,
                validPeriodDays
            );


            await RefreshMarketDataAsync();
        }


        // ==========================================
        // MARKET DATA REFRESH
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


            CancellationToken cancellationToken =
                _refreshCancellation.Token;


            string pair =
                store.Pair.Trim().ToUpperInvariant();


            int selectedPeriodDays =
                store.PeriodDays > 0
                    ? store.PeriodDays
                    : DefaultPeriodDays;


            MarketDataStore.Instance.SetLoading();


            ShowLoadingStatus(
                $"Loading {GetMarketDisplayName(pair)}..."
            );


            try
            {
                MarketDataResult rawResult =
                    await _marketDataService
                        .GetMarketDataAsync(
                            pair,
                            DailyInterval,
                            cancellationToken
                        );


                if (!rawResult.Success)
                {
                    MarketDataStore.Instance.SetResult(
                        rawResult
                    );


                    ShowErrorStatus(
                        rawResult.Error
                        ??
                        "Market data could not be loaded."
                    );

                    return;
                }


                List<MarketCandle> filteredCandles =
                    FilterCandlesByPeriod(
                        rawResult.Candles,
                        selectedPeriodDays
                    );


                MarketDataResult filteredResult =
                    CreateFilteredMarketResult(
                        rawResult,
                        filteredCandles
                    );


                MarketDataStore.Instance.SetResult(
                    filteredResult
                );


                ShowSuccessStatus(
                    filteredResult.Pair,
                    filteredResult.Candles.Count
                );
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


        private static List<MarketCandle>
            FilterCandlesByPeriod(
                IReadOnlyList<MarketCandle> candles,
                int periodDays)
        {
            if (candles.Count == 0)
            {
                return new List<MarketCandle>();
            }


            List<MarketCandle> orderedCandles =
                candles
                    .OrderBy(
                        candle =>
                            candle.Date
                    )
                    .ToList();


            if (periodDays <= 0)
            {
                return orderedCandles;
            }


            DateTimeOffset latestDate =
                orderedCandles[^1].Date;


            DateTimeOffset earliestDate =
                latestDate.AddDays(
                    -periodDays
                );


            return orderedCandles
                .Where(
                    candle =>
                        candle.Date >=
                        earliestDate
                )
                .ToList();
        }


        private static MarketDataResult
            CreateFilteredMarketResult(
                MarketDataResult source,
                List<MarketCandle> candles)
        {
            return new MarketDataResult
            {
                Success =
                    source.Success,

                Error =
                    source.Error,

                Pair =
                    source.Pair,

                Interval =
                    source.Interval,

                Candles =
                    candles
            };
        }


        // ==========================================
        // SWITCH FROM EXCEL TO API
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
        // GLOBAL DATA STORE
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
                (
                    store.DataSource
                    ??
                    ""
                ).StartsWith(
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
                        &&
                        string.Equals(
                            market.QuoteAsset,
                            _preferences.QuoteCurrency,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            if (selectedMarket is null)
            {
                ShowErrorStatus(
                    "The selected market is not available " +
                    "for the preferred quote currency."
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

            _preferences.PeriodDays =
                periodDays;

            _preferences.IntervalMinutes =
                DailyInterval;


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
                $"{store.Candles.Count:N0} imported observations";


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


            string requestedQuoteCurrency =
                e.QuoteCurrency
                    .Trim()
                    .ToUpperInvariant();


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


            // Preserve the current base asset when
            // switching the quote currency.
            //
            // Example:
            // ETH/USD -> ETH/EUR

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
                                requestedQuoteCurrency,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );
            }


            // Otherwise prefer ETH with the newly
            // selected quote currency.

            selectedMarket ??=
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.BaseAsset,
                            DefaultBaseAsset,
                            StringComparison.OrdinalIgnoreCase
                        )
                        &&
                        string.Equals(
                            market.QuoteAsset,
                            requestedQuoteCurrency,
                            StringComparison.OrdinalIgnoreCase
                        )
                );


            // Otherwise use the first available
            // market for that quote currency.

            selectedMarket ??=
                store.Markets.FirstOrDefault(
                    market =>
                        string.Equals(
                            market.QuoteAsset,
                            requestedQuoteCurrency,
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

            _preferences.IntervalMinutes =
                DailyInterval;


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
        // USER INTERFACE
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
                // The user cancelled account selection.
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
        // MARKET DISPLAY
        // ==========================================

        private string GetMarketDisplayName(
            string pair)
        {
            KrakenMarket? market =
                MarketDataStore.Instance
                    .Markets
                    .FirstOrDefault(
                        item =>
                            string.Equals(
                                item.Pair,
                                pair,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );


            if (
                market is not null
                &&
                !string.IsNullOrWhiteSpace(
                    market.DisplayName
                )
            )
            {
                return market.DisplayName;
            }


            return pair;
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
                $"{count:N0} daily observations";


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


            _marketDataService.Dispose();


            base.OnClosed(e);
        }
    }
}
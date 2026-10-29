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

            Left = 0;
            Top = 0;

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
                WindowState == WindowState.Maximized
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
                WindowState == WindowState.Maximized
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

            _themeService.ApplyTheme(
                _preferences.Theme
            );
        }


        private async Task SaveCurrentUserPreferencesAsync()
        {
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
                                market.Pair ==
                                _preferences.MarketPair
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
                                market.QuoteAsset ==
                                _preferences.QuoteCurrency
                        );
                }


                selectedMarket ??=
                    result.Markets.FirstOrDefault();


                // ----------------------------------
                // RESTORE INTERVAL
                // ----------------------------------

                MarketInterval? selectedInterval =
                    null;

                if (
                    _preferences.IntervalMinutes.HasValue
                )
                {
                    selectedInterval =
                        result.Intervals.FirstOrDefault(
                            interval =>
                                interval.Minutes ==
                                _preferences.IntervalMinutes.Value
                        );
                }


                selectedInterval ??=
                    result.Intervals.FirstOrDefault(
                        interval =>
                            interval.IsDefault
                    )
                    ??
                    result.Intervals.FirstOrDefault();


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
                    )
                    ??
                    result.Periods.FirstOrDefault();


                // ----------------------------------
                // VALIDATE
                // ----------------------------------

                if (
                    selectedMarket is null ||
                    selectedInterval is null ||
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
                    selectedInterval.Minutes;

                _preferences.PeriodDays =
                    selectedPeriod.Days;


                await SaveCurrentUserPreferencesAsync();


                // ----------------------------------
                // LOAD MARKET DATA
                // ----------------------------------

                await ApplySelectionAndRefreshAsync(
                    selectedMarket.Pair,
                    selectedInterval.Minutes,
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
            int interval,
            int periodDays)
        {
            MarketDataStore.Instance.SetSelection(
                pair,
                interval,
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
                ||
                store.Interval is null
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

            int interval =
                store.Interval.Value;


            MarketDataStore.Instance.SetLoading();

            ShowLoadingStatus(
                $"Loading {pair}..."
            );


            try
            {
                MarketDataResult result =
                    await _marketDataService
                        .GetMarketDataAsync(
                            pair,
                            interval,
                            _refreshCancellation.Token
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
                new ChartView();

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
                        market.Pair ==
                        e.Pair
                );


            _preferences.MarketPair =
                e.Pair;

            _preferences.QuoteCurrency =
                selectedMarket?.QuoteAsset;

            _preferences.IntervalMinutes =
                e.Interval;

            _preferences.PeriodDays =
                e.PeriodDays;


            await SaveCurrentUserPreferencesAsync();


            await ApplySelectionAndRefreshAsync(
                e.Pair,
                e.Interval,
                e.PeriodDays
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
                        market.Pair ==
                        store.Pair
                );


            KrakenMarket? selectedMarket =
                null;


            if (currentMarket is not null)
            {
                selectedMarket =
                    store.Markets.FirstOrDefault(
                        market =>
                            market.BaseAsset ==
                                currentMarket.BaseAsset
                            &&
                            market.QuoteAsset ==
                                e.QuoteCurrency
                    );
            }


            selectedMarket ??=
                store.Markets.FirstOrDefault(
                    market =>
                        market.QuoteAsset ==
                        e.QuoteCurrency
                );


            if (selectedMarket is null)
            {
                ShowErrorStatus(
                    "No market is available for the selected currency."
                );

                return;
            }


            _preferences.QuoteCurrency =
                e.QuoteCurrency;

            _preferences.MarketPair =
                selectedMarket.Pair;

            _preferences.IntervalMinutes =
                e.Interval;


            await SaveCurrentUserPreferencesAsync();


            int period =
                store.PeriodDays;


            if (period == 0)
            {
                MarketPeriod? defaultPeriod =
                    store.Periods.FirstOrDefault(
                        item =>
                            item.IsDefault
                    )
                    ??
                    store.Periods.FirstOrDefault();


                if (defaultPeriod is not null)
                {
                    period =
                        defaultPeriod.Days;
                }
            }


            await ApplySelectionAndRefreshAsync(
                selectedMarket.Pair,
                e.Interval,
                period
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


                // ----------------------------------
                // LOAD THIS USER'S PREFERENCES
                // ----------------------------------

                await LoadCurrentUserPreferencesAsync();


                UpdateUserInterface();


                // ----------------------------------
                // RESTORE THIS USER'S MARKET
                // ----------------------------------

                MarketDataStore.Instance
                    .ClearImportedData();


                await LoadMetadataAsync();


                ShowDashboard();
            }
            catch (MsalClientException error)
                when (IsAuthenticationCancellation(error))
            {
                // User cancelled.
                // Leave the application in its
                // existing state.
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


                // ----------------------------------
                // LOAD GUEST PREFERENCES
                // ----------------------------------

                await LoadCurrentUserPreferencesAsync();


                UpdateUserInterface();


                // ----------------------------------
                // RETURN TO GUEST DATA SETTINGS
                // ----------------------------------

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
                $"{pair} · {count} observations";


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
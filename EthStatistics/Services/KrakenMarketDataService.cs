using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EthStatistics.Models;

namespace EthStatistics.Services
{
    public sealed class KrakenMarketDataService
        : IDisposable
    {
        // ==========================================
        // CONSTANTS
        // ==========================================

        private const string AssetPairsEndpoint =
            "0/public/AssetPairs";

        private const string OhlcEndpoint =
            "0/public/OHLC";

        private const int DailyInterval =
            1440;


        private static readonly HashSet<int>
            ValidIntervals =
            new()
            {
                1,
                5,
                15,
                30,
                60,
                240,
                1440,
                10080,
                21600
            };


        private static readonly IReadOnlyDictionary<
            string,
            string
        > AssetAliases =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            )
            {
                // Bitcoin

                ["XXBT"] =
                    "BTC",

                ["XBT"] =
                    "BTC",

                ["BTC"] =
                    "BTC",


                // Ethereum

                ["XETH"] =
                    "ETH",

                ["ETH"] =
                    "ETH",


                // Litecoin

                ["XLTC"] =
                    "LTC",

                ["LTC"] =
                    "LTC",


                // XRP

                ["XXRP"] =
                    "XRP",

                ["XRP"] =
                    "XRP",


                // Dogecoin

                ["XXDG"] =
                    "DOGE",

                ["XDG"] =
                    "DOGE",

                ["DOGE"] =
                    "DOGE",


                // Other Kraken legacy names

                ["XETC"] =
                    "ETC",

                ["ETC"] =
                    "ETC",

                ["XMLN"] =
                    "MLN",

                ["MLN"] =
                    "MLN",

                ["XREP"] =
                    "REP",

                ["REP"] =
                    "REP",


                // Fiat currencies

                ["ZUSD"] =
                    "USD",

                ["USD"] =
                    "USD",

                ["ZEUR"] =
                    "EUR",

                ["EUR"] =
                    "EUR",

                ["ZGBP"] =
                    "GBP",

                ["GBP"] =
                    "GBP",

                ["ZJPY"] =
                    "JPY",

                ["JPY"] =
                    "JPY",

                ["ZCAD"] =
                    "CAD",

                ["CAD"] =
                    "CAD",

                ["ZAUD"] =
                    "AUD",

                ["AUD"] =
                    "AUD",

                ["ZCHF"] =
                    "CHF",

                ["CHF"] =
                    "CHF"
            };


        // ==========================================
        // HTTP
        // ==========================================

        private readonly HttpClient
            _httpClient;

        private readonly bool
            _ownsHttpClient;


        // ==========================================
        // CACHE
        // ==========================================

        private MarketMetadataResult?
            _metadataCache;

        private readonly Dictionary<
            MarketCacheKey,
            MarketDataResult
        > _marketDataCache =
            new();


        private readonly SemaphoreSlim
            _metadataLock =
                new(
                    1,
                    1
                );

        private readonly SemaphoreSlim
            _marketDataLock =
                new(
                    1,
                    1
                );


        // ==========================================
        // CONSTRUCTORS
        // ==========================================

        public KrakenMarketDataService()
        {
            _httpClient =
                CreateHttpClient();

            _ownsHttpClient =
                true;
        }


        public KrakenMarketDataService(
            HttpClient httpClient)
        {
            _httpClient =
                httpClient
                ??
                throw new ArgumentNullException(
                    nameof(httpClient)
                );

            _ownsHttpClient =
                false;
        }


        private static HttpClient CreateHttpClient()
        {
            var httpClient =
                new HttpClient
                {
                    BaseAddress =
                        new Uri(
                            "https://api.kraken.com/"
                        ),

                    Timeout =
                        TimeSpan.FromSeconds(
                            30
                        )
                };


            httpClient.DefaultRequestHeaders
                .UserAgent
                .ParseAdd(
                    "EthStatistics/1.0"
                );


            httpClient.DefaultRequestHeaders
                .Accept
                .ParseAdd(
                    "application/json"
                );


            return httpClient;
        }


        // ==========================================
        // METADATA
        // ==========================================

        public async Task<MarketMetadataResult>
            GetMetadataAsync(
                CancellationToken cancellationToken = default)
        {
            if (_metadataCache is not null)
            {
                return _metadataCache;
            }


            await _metadataLock.WaitAsync(
                cancellationToken
            );


            try
            {
                if (_metadataCache is not null)
                {
                    return _metadataCache;
                }


                using JsonDocument document =
                    await GetJsonAsync(
                        AssetPairsEndpoint,
                        cancellationToken
                    );


                JsonElement root =
                    document.RootElement;


                ThrowIfKrakenReturnedErrors(
                    root
                );


                if (
                    !root.TryGetProperty(
                        "result",
                        out JsonElement resultElement
                    )
                    ||
                    resultElement.ValueKind !=
                    JsonValueKind.Object
                )
                {
                    return CreateMetadataError(
                        "Kraken returned no asset-pair metadata."
                    );
                }


                var uniqueMarkets =
                    new Dictionary<
                        string,
                        KrakenMarket
                    >(
                        StringComparer.OrdinalIgnoreCase
                    );


                foreach (
                    JsonProperty pairProperty
                    in resultElement.EnumerateObject()
                )
                {
                    KrakenMarket? market =
                        ParseMarket(
                            pairProperty.Name,
                            pairProperty.Value
                        );


                    if (
                        market is null ||
                        string.IsNullOrWhiteSpace(
                            market.Pair
                        )
                    )
                    {
                        continue;
                    }


                    uniqueMarkets.TryAdd(
                        market.Pair,
                        market
                    );
                }


                List<KrakenMarket> markets =
                    uniqueMarkets.Values
                        .OrderBy(
                            market =>
                                market.QuoteAsset
                        )
                        .ThenBy(
                            market =>
                                market.BaseAsset
                        )
                        .ThenBy(
                            market =>
                                market.DisplayName
                        )
                        .ToList();


                if (markets.Count == 0)
                {
                    return CreateMetadataError(
                        "Kraken returned no usable online markets."
                    );
                }


                List<string> quoteCurrencies =
                    markets
                        .Select(
                            market =>
                                market.QuoteAsset
                        )
                        .Where(
                            value =>
                                !string.IsNullOrWhiteSpace(
                                    value
                                )
                        )
                        .Distinct(
                            StringComparer.OrdinalIgnoreCase
                        )
                        .OrderBy(
                            value =>
                                value
                        )
                        .ToList();


                _metadataCache =
                    new MarketMetadataResult
                    {
                        Success =
                            true,

                        Error =
                            null,

                        Markets =
                            markets,

                        QuoteCurrencies =
                            quoteCurrencies,

                        Intervals =
                            CreateIntervals(),

                        Periods =
                            CreatePeriods()
                    };


                return _metadataCache;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                return CreateMetadataError(
                    GetFriendlyErrorMessage(
                        error
                    )
                );
            }
            finally
            {
                _metadataLock.Release();
            }
        }


        private static KrakenMarket? ParseMarket(
            string internalPair,
            JsonElement pairElement)
        {
            if (
                pairElement.ValueKind !=
                JsonValueKind.Object
            )
            {
                return null;
            }


            string status =
                GetString(
                    pairElement,
                    "status"
                );


            if (
                !string.IsNullOrWhiteSpace(
                    status
                )
                &&
                !string.Equals(
                    status,
                    "online",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return null;
            }


            string restPair =
                GetString(
                    pairElement,
                    "altname"
                );


            if (
                string.IsNullOrWhiteSpace(
                    restPair
                )
            )
            {
                restPair =
                    internalPair;
            }


            restPair =
                restPair
                    .Trim()
                    .ToUpperInvariant();


            if (
                string.IsNullOrWhiteSpace(
                    restPair
                )
            )
            {
                return null;
            }


            string webSocketName =
                GetString(
                    pairElement,
                    "wsname"
                );


            string baseAsset;
            string quoteAsset;


            if (
                TrySplitDisplayPair(
                    webSocketName,
                    out string displayBase,
                    out string displayQuote
                )
            )
            {
                baseAsset =
                    NormalizeAsset(
                        displayBase
                    );

                quoteAsset =
                    NormalizeAsset(
                        displayQuote
                    );
            }
            else
            {
                baseAsset =
                    NormalizeAsset(
                        GetString(
                            pairElement,
                            "base"
                        )
                    );

                quoteAsset =
                    NormalizeAsset(
                        GetString(
                            pairElement,
                            "quote"
                        )
                    );
            }


            if (
                string.IsNullOrWhiteSpace(
                    baseAsset
                )
                ||
                string.IsNullOrWhiteSpace(
                    quoteAsset
                )
            )
            {
                return null;
            }


            return new KrakenMarket
            {
                Pair =
                    restPair,

                DisplayName =
                    $"{baseAsset}/{quoteAsset}",

                BaseAsset =
                    baseAsset,

                QuoteAsset =
                    quoteAsset
            };
        }


        // ==========================================
        // MARKET DATA
        // ==========================================

        public async Task<MarketDataResult>
            GetMarketDataAsync(
                string pair,
                int interval,
                CancellationToken cancellationToken = default)
        {
            string normalizedPair =
                ValidatePair(
                    pair
                );


            int normalizedInterval =
                ValidateInterval(
                    interval
                );


            var cacheKey =
                new MarketCacheKey(
                    normalizedPair,
                    normalizedInterval
                );


            if (
                _marketDataCache.TryGetValue(
                    cacheKey,
                    out MarketDataResult? cachedResult
                )
            )
            {
                return cachedResult;
            }


            await _marketDataLock.WaitAsync(
                cancellationToken
            );


            try
            {
                if (
                    _marketDataCache.TryGetValue(
                        cacheKey,
                        out cachedResult
                    )
                )
                {
                    return cachedResult;
                }


                string requestUri =
                    OhlcEndpoint +
                    "?pair=" +
                    Uri.EscapeDataString(
                        normalizedPair
                    )
                    +
                    "&interval=" +
                    normalizedInterval.ToString(
                        CultureInfo.InvariantCulture
                    );


                using JsonDocument document =
                    await GetJsonAsync(
                        requestUri,
                        cancellationToken
                    );


                JsonElement root =
                    document.RootElement;


                ThrowIfKrakenReturnedErrors(
                    root
                );


                if (
                    !root.TryGetProperty(
                        "result",
                        out JsonElement resultElement
                    )
                    ||
                    resultElement.ValueKind !=
                    JsonValueKind.Object
                )
                {
                    return CreateMarketDataError(
                        normalizedPair,
                        normalizedInterval,
                        "Kraken returned no OHLC result."
                    );
                }


                JsonElement candleArray =
                    default;


                bool foundSeries =
                    false;


                foreach (
                    JsonProperty property
                    in resultElement.EnumerateObject()
                )
                {
                    if (
                        string.Equals(
                            property.Name,
                            "last",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }


                    if (
                        property.Value.ValueKind ==
                        JsonValueKind.Array
                    )
                    {
                        candleArray =
                            property.Value;

                        foundSeries =
                            true;

                        break;
                    }
                }


                if (!foundSeries)
                {
                    return CreateMarketDataError(
                        normalizedPair,
                        normalizedInterval,
                        "Kraken returned no OHLC series."
                    );
                }


                List<MarketCandle> candles =
                    ParseCandles(
                        candleArray
                    );


                // Kraken's last entry represents the
                // current, incomplete timeframe.

                if (candles.Count > 0)
                {
                    candles.RemoveAt(
                        candles.Count - 1
                    );
                }


                if (candles.Count == 0)
                {
                    return CreateMarketDataError(
                        normalizedPair,
                        normalizedInterval,
                        "Kraken returned no completed candles."
                    );
                }


                candles =
                    candles
                        .OrderBy(
                            candle =>
                                candle.Date
                        )
                        .ToList();


                var result =
                    new MarketDataResult
                    {
                        Success =
                            true,

                        Error =
                            null,

                        Pair =
                            normalizedPair,

                        Interval =
                            normalizedInterval,

                        Candles =
                            candles
                    };


                _marketDataCache[cacheKey] =
                    result;


                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception error)
            {
                return CreateMarketDataError(
                    normalizedPair,
                    normalizedInterval,
                    GetFriendlyErrorMessage(
                        error
                    )
                );
            }
            finally
            {
                _marketDataLock.Release();
            }
        }


        private static List<MarketCandle> ParseCandles(
            JsonElement candleArray)
        {
            var candles =
                new List<MarketCandle>();


            foreach (
                JsonElement item
                in candleArray.EnumerateArray()
            )
            {
                if (
                    item.ValueKind !=
                    JsonValueKind.Array
                )
                {
                    continue;
                }


                JsonElement.ArrayEnumerator values =
                    item.EnumerateArray();


                var fields =
                    new List<JsonElement>(
                        8
                    );


                while (values.MoveNext())
                {
                    fields.Add(
                        values.Current
                    );
                }


                if (fields.Count < 8)
                {
                    continue;
                }


                long unixTimestamp =
                    ReadInt64(
                        fields[0]
                    );


                double open =
                    ReadDouble(
                        fields[1]
                    );


                double high =
                    ReadDouble(
                        fields[2]
                    );


                double low =
                    ReadDouble(
                        fields[3]
                    );


                double close =
                    ReadDouble(
                        fields[4]
                    );


                double vwap =
                    ReadDouble(
                        fields[5]
                    );


                double volume =
                    ReadDouble(
                        fields[6]
                    );


                int tradeCount =
     checked(
         (int)ReadInt64(
             fields[7]
         )
     
     );


                if (
                    !double.IsFinite(open) ||
                    !double.IsFinite(high) ||
                    !double.IsFinite(low) ||
                    !double.IsFinite(close) ||
                    !double.IsFinite(vwap) ||
                    !double.IsFinite(volume)
                )
                {
                    continue;
                }


                candles.Add(
                    new MarketCandle
                    {
                        Date =
                            DateTimeOffset
                                .FromUnixTimeSeconds(
                                    unixTimestamp
                                ),

                        Open =
                            open,

                        High =
                            high,

                        Low =
                            low,

                        Close =
                            close,

                        Vwap =
                            vwap,

                        Volume =
                            volume,

                        TradeCount =
                            tradeCount
                    }
                );
            }


            return candles;
        }


        // ==========================================
        // CACHE MANAGEMENT
        // ==========================================

        public void InvalidateMarketDataCache(
            string? pair = null)
        {
            if (
                string.IsNullOrWhiteSpace(
                    pair
                )
            )
            {
                _marketDataCache.Clear();

                return;
            }


            string normalizedPair =
                pair
                    .Trim()
                    .ToUpperInvariant();


            List<MarketCacheKey> keysToRemove =
                _marketDataCache.Keys
                    .Where(
                        key =>
                            string.Equals(
                                key.Pair,
                                normalizedPair,
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .ToList();


            foreach (
                MarketCacheKey key
                in keysToRemove
            )
            {
                _marketDataCache.Remove(
                    key
                );
            }
        }


        public void InvalidateMetadataCache()
        {
            _metadataCache =
                null;
        }


        // ==========================================
        // METADATA DEFINITIONS
        // ==========================================

        private static List<MarketInterval>
            CreateIntervals()
        {
            return
            [
                new MarketInterval
                {
                    Name =
                        "1 Minute",

                    Minutes =
                        1,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "5 Minutes",

                    Minutes =
                        5,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "15 Minutes",

                    Minutes =
                        15,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "30 Minutes",

                    Minutes =
                        30,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "1 Hour",

                    Minutes =
                        60,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "4 Hours",

                    Minutes =
                        240,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "1 Day",

                    Minutes =
                        DailyInterval,

                    IsDefault =
                        true
                },

                new MarketInterval
                {
                    Name =
                        "1 Week",

                    Minutes =
                        10080,

                    IsDefault =
                        false
                },

                new MarketInterval
                {
                    Name =
                        "15 Days",

                    Minutes =
                        21600,

                    IsDefault =
                        false
                }
            ];
        }


        private static List<MarketPeriod>
            CreatePeriods()
        {
            return
            [
                new MarketPeriod
                {
                    Name =
                        "30 Days",

                    Days =
                        30,

                    IsDefault =
                        false
                },

                new MarketPeriod
                {
                    Name =
                        "90 Days",

                    Days =
                        90,

                    IsDefault =
                        false
                },

                new MarketPeriod
                {
                    Name =
                        "180 Days",

                    Days =
                        180,

                    IsDefault =
                        false
                },

                new MarketPeriod
                {
                    Name =
                        "1 Year",

                    Days =
                        365,

                    IsDefault =
                        true
                },

                new MarketPeriod
                {
                    Name =
                        "Maximum",

                    Days =
                        720,

                    IsDefault =
                        false
                }
            ];
        }


        // ==========================================
        // JSON HELPERS
        // ==========================================

        private async Task<JsonDocument> GetJsonAsync(
            string requestUri,
            CancellationToken cancellationToken)
        {
            try
            {
                using HttpResponseMessage response =
                    await _httpClient.GetAsync(
                        requestUri,
                        HttpCompletionOption
                            .ResponseHeadersRead,
                        cancellationToken
                    );


                if (
                    response.StatusCode ==
                    HttpStatusCode.TooManyRequests
                )
                {
                    throw new InvalidOperationException(
                        "Kraken temporarily limited the request rate."
                    );
                }


                response.EnsureSuccessStatusCode();


                await using System.IO.Stream stream =
                    await response.Content
                        .ReadAsStreamAsync(
                            cancellationToken
                        );


                return await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken:
                        cancellationToken
                );
            }
            catch (TaskCanceledException)
                when (
                    !cancellationToken
                        .IsCancellationRequested
                )
            {
                throw new TimeoutException(
                    "The Kraken request timed out."
                );
            }
            catch (HttpRequestException error)
            {
                throw new InvalidOperationException(
                    "Kraken could not be reached. " +
                    "Check the internet connection.",
                    error
                );
            }
        }


        private static void ThrowIfKrakenReturnedErrors(
            JsonElement root)
        {
            if (
                !root.TryGetProperty(
                    "error",
                    out JsonElement errorsElement
                )
                ||
                errorsElement.ValueKind !=
                JsonValueKind.Array
            )
            {
                return;
            }


            List<string> errors =
                errorsElement
                    .EnumerateArray()
                    .Where(
                        item =>
                            item.ValueKind ==
                            JsonValueKind.String
                    )
                    .Select(
                        item =>
                            item.GetString()
                            ??
                            ""
                    )
                    .Where(
                        value =>
                            !string.IsNullOrWhiteSpace(
                                value
                            )
                    )
                    .ToList();


            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Kraken API error: " +
                    string.Join(
                        ", ",
                        errors
                    )
                );
            }
        }


        private static string GetString(
            JsonElement element,
            string propertyName)
        {
            if (
                element.TryGetProperty(
                    propertyName,
                    out JsonElement property
                )
                &&
                property.ValueKind ==
                JsonValueKind.String
            )
            {
                return
                    property.GetString()
                    ??
                    "";
            }


            return "";
        }


        private static double ReadDouble(
            JsonElement element)
        {
            if (
                element.ValueKind ==
                JsonValueKind.Number
                &&
                element.TryGetDouble(
                    out double number
                )
            )
            {
                return number;
            }


            if (
                element.ValueKind ==
                JsonValueKind.String
                &&
                double.TryParse(
                    element.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out number
                )
            )
            {
                return number;
            }


            throw new FormatException(
                "Kraken returned an invalid numeric value."
            );
        }


        private static long ReadInt64(
            JsonElement element)
        {
            if (
                element.ValueKind ==
                JsonValueKind.Number
                &&
                element.TryGetInt64(
                    out long number
                )
            )
            {
                return number;
            }


            if (
                element.ValueKind ==
                JsonValueKind.String
                &&
                long.TryParse(
                    element.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out number
                )
            )
            {
                return number;
            }


            throw new FormatException(
                "Kraken returned an invalid integer value."
            );
        }


        // ==========================================
        // NORMALIZATION
        // ==========================================

        private static bool TrySplitDisplayPair(
            string displayPair,
            out string baseAsset,
            out string quoteAsset)
        {
            baseAsset =
                "";

            quoteAsset =
                "";


            if (
                string.IsNullOrWhiteSpace(
                    displayPair
                )
            )
            {
                return false;
            }


            string[] parts =
                displayPair.Split(
                    '/',
                    2,
                    StringSplitOptions
                        .RemoveEmptyEntries
                    |
                    StringSplitOptions
                        .TrimEntries
                );


            if (parts.Length != 2)
            {
                return false;
            }


            baseAsset =
                parts[0];

            quoteAsset =
                parts[1];


            return true;
        }


        private static string NormalizeAsset(
            string asset)
        {
            string normalized =
                asset
                    .Trim()
                    .ToUpperInvariant();


            if (
                AssetAliases.TryGetValue(
                    normalized,
                    out string? alias
                )
            )
            {
                return alias;
            }


            return normalized;
        }


        private static string ValidatePair(
            string pair)
        {
            if (
                string.IsNullOrWhiteSpace(
                    pair
                )
            )
            {
                throw new ArgumentException(
                    "A market pair is required.",
                    nameof(pair)
                );
            }


            return
                pair
                    .Trim()
                    .ToUpperInvariant();
        }


        private static int ValidateInterval(
            int interval)
        {
            if (
                !ValidIntervals.Contains(
                    interval
                )
            )
            {
                throw new ArgumentOutOfRangeException(
                    nameof(interval),
                    interval,
                    "The selected Kraken interval is not supported."
                );
            }


            return interval;
        }


        // ==========================================
        // ERROR RESULTS
        // ==========================================

        private static MarketMetadataResult
            CreateMetadataError(
                string error)
        {
            return new MarketMetadataResult
            {
                Success =
                    false,

                Error =
                    error,

                Markets =
                    [],

                QuoteCurrencies =
                    [],

                Intervals =
                    CreateIntervals(),

                Periods =
                    CreatePeriods()
            };
        }


        private static MarketDataResult
            CreateMarketDataError(
                string pair,
                int interval,
                string error)
        {
            return new MarketDataResult
            {
                Success =
                    false,

                Error =
                    error,

                Pair =
                    pair,

                Interval =
                    interval,

                Candles =
                    []
            };
        }


        private static string GetFriendlyErrorMessage(
            Exception error)
        {
            return error switch
            {
                TimeoutException =>
                    error.Message,

                InvalidOperationException =>
                    error.Message,

                JsonException =>
                    "Kraken returned invalid JSON.",

                FormatException =>
                    error.Message,

                _ =>
                    error.Message
            };
        }


        // ==========================================
        // CLEANUP
        // ==========================================

        public void Dispose()
        {
            _metadataLock.Dispose();

            _marketDataLock.Dispose();


            if (_ownsHttpClient)
            {
                _httpClient.Dispose();
            }
        }


        // ==========================================
        // CACHE KEY
        // ==========================================

        private readonly record struct MarketCacheKey(
            string Pair,
            int Interval
        );
    }
}
using EthStatistics.Models;
using System.IO;

namespace EthStatistics.Services
{
    public sealed class MarketDataStore
    {
        public string DataSource { get; private set; } =
    "Provider";

        public IReadOnlyList<double> ImportedReturns
        {
            get;
            private set;
        } = [];

        public ImportedValueType? ImportedValueType
        {
            get;
            private set;
        }
        public void SetImportedData(
    ExcelImportResult result)
        {
            Pair =
                Path.GetFileNameWithoutExtension(
                    result.FileName
                );

            Interval =
                result.IntervalMinutes;

            PeriodDays = 0;

            Candles =
                result.Candles;

            ImportedReturns =
                result.ImportedReturns;

            ImportedValueType =
                result.ValueType;

            DataSource =
                $"Excel · {result.SheetName}";

            Error = null;
            IsLoading = false;

            Changed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
        public static MarketDataStore Instance { get; } =
            new();

        private MarketDataStore()
        {
        }

        public IReadOnlyList<KrakenMarket> Markets { get; private set; }
            = [];

        public IReadOnlyList<string> QuoteCurrencies { get; private set; }
            = [];

        public IReadOnlyList<MarketInterval> Intervals { get; private set; }
            = [];

        public IReadOnlyList<MarketPeriod> Periods { get; private set; }
            = [];

        public string? Pair { get; private set; }

        public int? Interval { get; private set; }

        public int PeriodDays { get; private set; }

        public IReadOnlyList<MarketCandle> Candles { get; private set; }
            = [];

        public bool IsLoading { get; private set; }

        public string? Error { get; private set; }

        public event EventHandler? Changed;

        public void SetMetadata(
            MarketMetadataResult result)
        {
            Markets = result.Markets;
            QuoteCurrencies = result.QuoteCurrencies;
            Intervals = result.Intervals;
            Periods = result.Periods;
            Error = result.Success
                ? null
                : result.Error;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SetSelection(
            string pair,
            int interval,
            int periodDays)
        {
            Pair = pair;
            Interval = interval;
            PeriodDays = periodDays;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SetLoading()
        {
            Error = null;
            IsLoading = true;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void SetResult(
      MarketDataResult result)
        {
            Pair = result.Pair;
            Interval = result.Interval;
            Candles = result.Candles;

            ImportedReturns = [];
            ImportedValueType = null;
            DataSource = "Provider";

            Error = result.Success
                ? null
                : result.Error;

            IsLoading = false;

            Changed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
        public void ClearImportedData()
        {
            Candles = [];

            ImportedReturns = [];

            ImportedValueType = null;

            Pair = null;

            Interval = null;

            PeriodDays = 0;

            DataSource = "Provider";

            Error = null;

            IsLoading = false;

            Changed?.Invoke(
                this,
                EventArgs.Empty
            );
        }
        public void SetError(string error)
        {
            Error = error;
            IsLoading = false;

            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
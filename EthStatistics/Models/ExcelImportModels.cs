namespace EthStatistics.Models
{
    public sealed class ExcelSheetInfo
    {
        public required string Name { get; init; }

        public required IReadOnlyList<string> Columns { get; init; }

        public required IReadOnlyList<ExcelImportRow> Rows { get; init; }

        public override string ToString()
        {
            return Name;
        }
    }


    public sealed class ExcelImportRow
    {
        public required IReadOnlyDictionary<string, ExcelCellValue>
            Values
        { get; init; }
    }


    public sealed class ExcelCellValue
    {
        public object? Value { get; init; }

        public string? NumberFormat { get; init; }
    }


    public enum ImportedValueType
    {
        Price,
        PercentageReturn
    }


    public sealed class ExcelImportResult
    {
        public required string FileName { get; init; }

        public required string SheetName { get; init; }

        public required string DateColumn { get; init; }

        public required string ValueColumn { get; init; }

        public required ImportedValueType ValueType { get; init; }

        public required IReadOnlyList<MarketCandle> Candles { get; init; }

        public required IReadOnlyList<double> ImportedReturns { get; init; }

        public required int IntervalMinutes { get; init; }
    }
}
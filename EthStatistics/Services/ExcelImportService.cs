using System.Globalization;
using System.IO;
using System.Text;
using ExcelDataReader;
using EthStatistics.Models;

namespace EthStatistics.Services
{
    public sealed class ExcelImportService
    {
        public ExcelImportService()
        {
            Encoding.RegisterProvider(
                CodePagesEncodingProvider.Instance
            );
        }


        public IReadOnlyList<ExcelSheetInfo> ReadWorkbook(
            string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The selected Excel file does not exist.",
                    filePath
                );
            }

            using FileStream stream =
                File.Open(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );

            using IExcelDataReader reader =
                ExcelReaderFactory.CreateReader(stream);

            var sheets =
                new List<ExcelSheetInfo>();

            do
            {
                ExcelSheetInfo? sheet =
                    ReadCurrentSheet(reader);

                if (sheet is not null)
                {
                    sheets.Add(sheet);
                }
            }
            while (reader.NextResult());

            return sheets;
        }


        private static ExcelSheetInfo? ReadCurrentSheet(
            IExcelDataReader reader)
        {
            if (!reader.Read())
            {
                return null;
            }

            var columns = new List<string>();

            for (
                int columnIndex = 0;
                columnIndex < reader.FieldCount;
                columnIndex++)
            {
                string header =
                    reader.GetValue(columnIndex)?
                        .ToString()?
                        .Trim()
                    ?? "";

                if (string.IsNullOrWhiteSpace(header))
                {
                    header =
                        $"Column {columnIndex + 1}";
                }

                header =
                    MakeUniqueHeader(
                        header,
                        columns
                    );

                columns.Add(header);
            }

            var rows =
                new List<ExcelImportRow>();

            while (reader.Read())
            {
                var values =
                    new Dictionary<string, ExcelCellValue>();

                bool hasAnyValue = false;

                for (
                    int columnIndex = 0;
                    columnIndex < columns.Count;
                    columnIndex++)
                {
                    object? value =
                        reader.GetValue(columnIndex);

                    if (
                        value is not null &&
                        !string.IsNullOrWhiteSpace(
                            value.ToString())
                    )
                    {
                        hasAnyValue = true;
                    }

                    string? numberFormat = null;

                    try
                    {
                        numberFormat =
                            reader.GetNumberFormatString(
                                columnIndex
                            );
                    }
                    catch
                    {
                        // Formatting metadata is optional.
                    }

                    values[columns[columnIndex]] =
                        new ExcelCellValue
                        {
                            Value = value,
                            NumberFormat = numberFormat
                        };
                }

                if (hasAnyValue)
                {
                    rows.Add(
                        new ExcelImportRow
                        {
                            Values = values
                        }
                    );
                }
            }

            return new ExcelSheetInfo
            {
                Name = reader.Name,
                Columns = columns,
                Rows = rows
            };
        }


        public ImportedValueType DetectValueType(
            ExcelSheetInfo sheet,
            string valueColumn)
        {
            int percentageFormatted = 0;
            int percentageStrings = 0;
            int usableValues = 0;

            foreach (ExcelImportRow row in sheet.Rows)
            {
                if (
                    !row.Values.TryGetValue(
                        valueColumn,
                        out ExcelCellValue? cell) ||
                    cell.Value is null
                )
                {
                    continue;
                }

                usableValues++;

                string text =
                    cell.Value.ToString()?.Trim() ?? "";

                if (text.EndsWith("%"))
                {
                    percentageStrings++;
                }

                if (
                    cell.NumberFormat?.Contains('%') == true
                )
                {
                    percentageFormatted++;
                }
            }

            if (usableValues == 0)
            {
                throw new InvalidOperationException(
                    "The selected value column contains no usable values."
                );
            }

            return
                percentageFormatted > 0 ||
                percentageStrings > 0
                    ? ImportedValueType.PercentageReturn
                    : ImportedValueType.Price;
        }


        public ExcelImportResult Import(
            string filePath,
            ExcelSheetInfo sheet,
            string dateColumn,
            string valueColumn,
            ImportedValueType valueType)
        {
            var observations =
                new List<(DateTimeOffset Date, double Value)>();

            foreach (ExcelImportRow row in sheet.Rows)
            {
                if (
                    !row.Values.TryGetValue(
                        dateColumn,
                        out ExcelCellValue? dateCell) ||
                    !row.Values.TryGetValue(
                        valueColumn,
                        out ExcelCellValue? valueCell)
                )
                {
                    continue;
                }

                if (
                    !TryReadDate(
                        dateCell.Value,
                        out DateTimeOffset date)
                )
                {
                    continue;
                }

                if (
                    !TryReadNumber(
                        valueCell,
                        valueType,
                        out double value)
                )
                {
                    continue;
                }

                observations.Add(
                    (date, value)
                );
            }

            observations =
                observations
                    .OrderBy(item => item.Date)
                    .GroupBy(item => item.Date)
                    .Select(group => group.Last())
                    .ToList();

            if (observations.Count < 2)
            {
                throw new InvalidOperationException(
                    "At least two valid date/value rows are required."
                );
            }

            IReadOnlyList<MarketCandle> candles;
            IReadOnlyList<double> importedReturns;

            if (
                valueType ==
                ImportedValueType.PercentageReturn
            )
            {
                importedReturns =
                    observations
                        .Select(item => item.Value)
                        .ToArray();

                candles =
                    BuildCandlesFromReturns(
                        observations
                    );
            }
            else
            {
                importedReturns =
                    Array.Empty<double>();

                candles =
                    BuildCandlesFromPrices(
                        observations
                    );
            }

            return new ExcelImportResult
            {
                FileName =
                    Path.GetFileName(filePath),

                SheetName =
                    sheet.Name,

                DateColumn =
                    dateColumn,

                ValueColumn =
                    valueColumn,

                ValueType =
                    valueType,

                Candles =
                    candles,

                ImportedReturns =
                    importedReturns,

                IntervalMinutes =
                    InferIntervalMinutes(
                        observations.Select(
                            item => item.Date
                        )
                    )
            };
        }


        private static IReadOnlyList<MarketCandle>
            BuildCandlesFromPrices(
                IReadOnlyList<(
                    DateTimeOffset Date,
                    double Value)> observations)
        {
            return observations
                .Where(item => item.Value > 0)
                .Select(
                    item => new MarketCandle
                    {
                        Date = item.Date,
                        Open = item.Value,
                        High = item.Value,
                        Low = item.Value,
                        Close = item.Value,
                        Vwap = item.Value,
                        Volume = 0,
                        TradeCount = 0
                    }
                )
                .ToArray();
        }


        private static IReadOnlyList<MarketCandle>
            BuildCandlesFromReturns(
                IReadOnlyList<(
                    DateTimeOffset Date,
                    double Value)> observations)
        {
            var candles =
                new List<MarketCandle>();

            double indexValue = 100.0;

            foreach (var observation in observations)
            {
                double previousValue =
                    indexValue;

                indexValue *=
                    1.0 + observation.Value;

                if (
                    !double.IsFinite(indexValue) ||
                    indexValue <= 0)
                {
                    throw new InvalidOperationException(
                        "The return series produces an invalid cumulative index."
                    );
                }

                candles.Add(
                    new MarketCandle
                    {
                        Date = observation.Date,
                        Open = previousValue,
                        High = Math.Max(
                            previousValue,
                            indexValue
                        ),
                        Low = Math.Min(
                            previousValue,
                            indexValue
                        ),
                        Close = indexValue,
                        Vwap = indexValue,
                        Volume = 0,
                        TradeCount = 0
                    }
                );
            }

            return candles;
        }


        private static bool TryReadDate(
            object? value,
            out DateTimeOffset result)
        {
            if (value is DateTime dateTime)
            {
                result =
                    new DateTimeOffset(dateTime);

                return true;
            }

            if (
                value is double excelSerial &&
                excelSerial > 0
            )
            {
                try
                {
                    DateTime converted =
                        DateTime.FromOADate(
                            excelSerial
                        );

                    result =
                        new DateTimeOffset(converted);

                    return true;
                }
                catch
                {
                }
            }

            string text =
                value?.ToString()?.Trim() ?? "";

            return DateTimeOffset.TryParse(
                text,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AssumeLocal,
                out result
            )
            ||
            DateTimeOffset.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out result
            );
        }


        private static bool TryReadNumber(
            ExcelCellValue cell,
            ImportedValueType valueType,
            out double result)
        {
            result = 0;

            if (
                cell.Value is byte or
                short or
                int or
                long or
                float or
                double or
                decimal
            )
            {
                result =
                    Convert.ToDouble(
                        cell.Value,
                        CultureInfo.InvariantCulture
                    );

                return
                    double.IsFinite(result);
            }

            string text =
                cell.Value?
                    .ToString()?
                    .Trim()
                ?? "";

            bool containsPercent =
                text.EndsWith("%");

            text =
                text.Replace("%", "")
                    .Trim();

            bool parsed =
                double.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.CurrentCulture,
                    out result
                )
                ||
                double.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out result
                );

            if (!parsed)
            {
                return false;
            }

            if (
                valueType ==
                    ImportedValueType.PercentageReturn &&
                containsPercent
            )
            {
                result /= 100.0;
            }

            return double.IsFinite(result);
        }


        private static int InferIntervalMinutes(
            IEnumerable<DateTimeOffset> dates)
        {
            DateTimeOffset[] sorted =
                dates
                    .OrderBy(date => date)
                    .ToArray();

            double[] differences =
                sorted
                    .Zip(
                        sorted.Skip(1),
                        (first, second) =>
                            (
                                second - first
                            ).TotalMinutes
                    )
                    .Where(value => value > 0)
                    .OrderBy(value => value)
                    .ToArray();

            if (differences.Length == 0)
            {
                return 1;
            }

            double median =
                differences[
                    differences.Length / 2
                ];

            return Math.Max(
                1,
                (int)Math.Round(median)
            );
        }


        private static string MakeUniqueHeader(
            string header,
            ICollection<string> existingHeaders)
        {
            if (!existingHeaders.Contains(header))
            {
                return header;
            }

            int suffix = 2;

            string candidate;

            do
            {
                candidate =
                    $"{header} ({suffix})";

                suffix++;
            }
            while (
                existingHeaders.Contains(candidate)
            );

            return candidate;
        }
    }
}
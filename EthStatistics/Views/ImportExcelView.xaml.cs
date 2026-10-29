using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using EthStatistics.Models;
using EthStatistics.Services;

namespace EthStatistics.Views
{
    public partial class ImportExcelView : UserControl
    {
        private readonly ExcelImportService
            _excelImportService = new();

        private string? _selectedFilePath;

        private IReadOnlyList<ExcelSheetInfo>
            _sheets = [];


        public event EventHandler?
            ImportCompleted;


        public ImportExcelView()
        {
            InitializeComponent();

            ValueTypeComboBox.SelectedIndex = 0;
        }


        private void ChooseFile_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog =
                new OpenFileDialog
                {
                    Title =
                        "Select Excel Workbook",

                    Filter =
                        "Excel Workbooks (*.xlsx;*.xls;*.xlsb)|*.xlsx;*.xls;*.xlsb",

                    CheckFileExists =
                        true,

                    Multiselect =
                        false
                };


            if (dialog.ShowDialog() != true)
            {
                return;
            }


            try
            {
                _selectedFilePath =
                    dialog.FileName;


                SelectedFileText.Text =
                    _selectedFilePath;


                ImportStatusText.Text =
                    "Reading workbook...";


                _sheets =
                    _excelImportService.ReadWorkbook(
                        _selectedFilePath
                    );


                WorksheetComboBox.ItemsSource =
                    _sheets;


                if (_sheets.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The workbook contains no readable worksheets."
                    );
                }


                WorksheetComboBox.SelectedIndex =
                    0;


                MappingCard.IsEnabled =
                    true;


                ValidateImportState();


                ImportStatusText.Text =
                    $"{_sheets.Count} worksheet(s) loaded";
            }
            catch (Exception error)
            {
                ResetImport();

                MessageBox.Show(
                    error.Message,
                    "Excel Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }


        private void WorksheetComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (
                WorksheetComboBox.SelectedItem
                is not ExcelSheetInfo sheet
            )
            {
                return;
            }


            DateColumnComboBox.ItemsSource =
                sheet.Columns;


            ValueColumnComboBox.ItemsSource =
                sheet.Columns;


            DateColumnComboBox.SelectedIndex =
                -1;


            ValueColumnComboBox.SelectedIndex =
                -1;


            DetectedTypeDescriptionText.Text =
                "Select a value column to detect its type";


            ImportButton.IsEnabled =
                false;
        }


        private void ValueColumnComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (
                WorksheetComboBox.SelectedItem
                    is not ExcelSheetInfo sheet
                ||
                ValueColumnComboBox.SelectedItem
                    is not string valueColumn
            )
            {
                ValidateImportState();

                return;
            }


            try
            {
                ImportedValueType detectedType =
                    _excelImportService.DetectValueType(
                        sheet,
                        valueColumn
                    );


                ValueTypeComboBox.SelectedIndex =
                    detectedType ==
                    ImportedValueType.PercentageReturn
                        ? 1
                        : 0;


                DetectedTypeDescriptionText.Text =
                    detectedType ==
                    ImportedValueType.PercentageReturn
                        ? "The selected column appears to contain percentage returns."
                        : "The selected column appears to contain numeric prices.";
            }
            catch (Exception error)
            {
                DetectedTypeDescriptionText.Text =
                    error.Message;
            }


            ValidateImportState();
        }


        private void ImportButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (
                string.IsNullOrWhiteSpace(
                    _selectedFilePath)
                ||
                WorksheetComboBox.SelectedItem
                    is not ExcelSheetInfo sheet
                ||
                DateColumnComboBox.SelectedItem
                    is not string dateColumn
                ||
                ValueColumnComboBox.SelectedItem
                    is not string valueColumn
                ||
                ValueTypeComboBox.SelectedItem
                    is not ComboBoxItem valueTypeItem
            )
            {
                return;
            }


            try
            {
                ImportedValueType valueType =
                    Enum.Parse<ImportedValueType>(
                        valueTypeItem.Tag?
                            .ToString()
                        ?? nameof(
                            ImportedValueType.Price
                        )
                    );


                ExcelImportResult result =
                    _excelImportService.Import(
                        _selectedFilePath,
                        sheet,
                        dateColumn,
                        valueColumn,
                        valueType
                    );


                MarketDataStore.Instance.SetImportedData(
                    result
                );


                ImportStatusText.Text =
                    $"{result.Candles.Count} observations imported";


                ImportCompleted?.Invoke(
                    this,
                    EventArgs.Empty
                );
            }
            catch (Exception error)
            {
                MessageBox.Show(
                    error.Message,
                    "Excel Import Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }
        private void DateColumnComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            ValidateImportState();
        }

        private void ValidateImportState()
        {
            ImportButton.IsEnabled =
                !string.IsNullOrWhiteSpace(
                    _selectedFilePath)
                &&
                WorksheetComboBox.SelectedItem
                    is ExcelSheetInfo
                &&
                DateColumnComboBox.SelectedItem
                    is string
                &&
                ValueColumnComboBox.SelectedItem
                    is string;
        }


        private void ResetImport()
        {
            _selectedFilePath = null;
            _sheets = [];

            SelectedFileText.Text =
                "No file selected";

            WorksheetComboBox.ItemsSource =
                null;

            DateColumnComboBox.ItemsSource =
                null;

            ValueColumnComboBox.ItemsSource =
                null;

            MappingCard.IsEnabled =
                false;

            ImportButton.IsEnabled =
                false;

            ImportStatusText.Text =
                "";
        }
    }
}
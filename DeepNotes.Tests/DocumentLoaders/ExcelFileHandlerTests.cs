using DeepNotes.DataLoaders.FileHandlers;
using DocumentFormat.OpenXml;
using Xunit;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DeepNotes.Tests.DocumentLoaders;

public class ExcelFileHandlerTests : IDisposable
{
    private readonly string _testFilePath;

    public ExcelFileHandlerTests()
    {
        _testFilePath = Path.GetTempFileName() + ".xlsx";
        CreateTestWorkbook(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task LoadDocumentAsync_ValidExcel_ExtractsContent()
    {
        // Arrange
        var handler = new ExcelFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Test Content", document.Content);
        Assert.Equal(_testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentAsync_WithMultipleSheets_ExtractsAllContent()
    {
        // Arrange
        var handler = new ExcelFileHandler();
        var data = new[]
        {
            new[] { "Header1", "Header2" },
            new[] { "Value1", "Value2" },
            new[] { "Value3", "Value4" }
        };
        CreateSpreadsheetWithData(_testFilePath, data);

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Header1", document.Content);
        Assert.Contains("Value4", document.Content);
    }

    [Theory]
    [InlineData(".xlsx")]
    [InlineData(".xls")]
    public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var handler = new ExcelFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.True(result);
    }

    private void CreateTestWorkbook(string filePath)
    {
        using var spreadsheet = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheet.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Sheet1"
        });

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        var row = new Row { RowIndex = 1 };
        row.AppendChild(new Cell { CellValue = new CellValue("Test Content") });
        sheetData.AppendChild(row);
    }

    private void CreateSpreadsheetWithData(string filePath, string[][] data)
    {
        using var spreadsheet = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheet.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Sheet1"
        });

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        for (uint rowIdx = 0; rowIdx < data.Length; rowIdx++)
        {
            var row = new Row { RowIndex = rowIdx + 1 };
            for (uint colIdx = 0; colIdx < data[rowIdx].Length; colIdx++)
            {
                var cell = new Cell { CellReference = $"{GetColumnName(colIdx + 1)}{rowIdx + 1}" };
                cell.CellValue = new CellValue(data[rowIdx][colIdx]);
                cell.DataType = new EnumValue<CellValues>(CellValues.String);
                row.AppendChild(cell);
            }

            sheetData.AppendChild(row);
        }
    }

    private static string GetColumnName(uint columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }
}
namespace DeepNotes.Tests.DocumentLoaders;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DeepNotes.DataLoaders.FileHandlers;
using Xunit;

public class ExcelFileHandlerTests : IDisposable
{
    private readonly string _testFilePath;

    public ExcelFileHandlerTests()
    {
        _testFilePath = Path.GetTempFileName() + ".xlsx";
        CreateTestSpreadsheet(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ValidExcel_ReturnsContent()
    {
        // Arrange
        var handler = new ExcelFileHandler();

        // Act
        var content = await handler.ExtractTextAsync(_testFilePath);

        // Assert
        Assert.Contains("Test Cell Content", content);
        Assert.Contains("Sheet: Sheet1", content);
    }

    [Fact]
    public void ExtractMetadata_ValidExcel_ReturnsMetadata()
    {
        // Arrange
        var handler = new ExcelFileHandler();

        // Act
        var metadata = handler.ExtractMetadata(_testFilePath);

        // Assert
        Assert.Equal("1", metadata["SheetCount"]);
    }

    private void CreateTestSpreadsheet(string filePath)
    {
        using var spreadsheet = SpreadsheetDocument.Create(filePath, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheet.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();

        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        worksheetPart.Worksheet = new Worksheet(new SheetData());

        var sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
        var sheet = new Sheet() 
        { 
            Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Sheet1"
        };
        sheets.AppendChild(sheet);

        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        var row = new Row();
        var cell = new Cell() { DataType = CellValues.String, CellValue = new CellValue("Test Cell Content") };
        row.AppendChild(cell);
        sheetData!.AppendChild(row);
    }
} 
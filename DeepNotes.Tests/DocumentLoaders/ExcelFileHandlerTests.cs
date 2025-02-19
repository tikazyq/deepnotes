namespace DeepNotes.Tests.DocumentLoaders;

using DeepNotes.DataLoaders.FileHandlers;
using ClosedXML.Excel;
using Xunit;

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
        Assert.Contains("Sheet1", document.Metadata["SheetNames"]);
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
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        worksheet.Cell("A1").Value = "Test Content";
        workbook.SaveAs(filePath);
    }

    private void CreateSpreadsheetWithData(string filePath, string[][] data)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Sheet1");
        
        for (int row = 0; row < data.Length; row++)
        {
            for (int col = 0; col < data[row].Length; col++)
            {
                worksheet.Cell(row + 1, col + 1).Value = data[row][col];
            }
        }
        
        workbook.SaveAs(filePath);
    }
} 
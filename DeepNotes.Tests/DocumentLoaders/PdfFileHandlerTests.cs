namespace DeepNotes.Tests.DocumentLoaders;

using DeepNotes.DataLoaders.FileHandlers;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Xunit;

public class PdfFileHandlerTests : IDisposable
{
    private readonly string _testFilePath;

    public PdfFileHandlerTests()
    {
        _testFilePath = Path.GetTempFileName() + ".pdf";
        CreateTestPdf(_testFilePath, "Test PDF content");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task ExtractTextAsync_ValidPdf_ReturnsContent()
    {
        // Arrange
        var handler = new PdfFileHandler();

        // Act
        var content = await handler.ExtractTextAsync(_testFilePath);

        // Assert
        Assert.Contains("Test PDF content", content);
    }

    [Fact]
    public void ExtractMetadata_ValidPdf_ReturnsMetadata()
    {
        // Arrange
        var handler = new PdfFileHandler();

        // Act
        var metadata = handler.ExtractMetadata(_testFilePath);

        // Assert
        Assert.Equal("1", metadata["PageCount"]);
        Assert.NotNull(metadata["PdfVersion"]);
    }

    private void CreateTestPdf(string filePath, string content)
    {
        using var writer = new PdfWriter(filePath);
        using var pdf = new PdfDocument(writer);
        var document = new iText.Layout.Document(pdf);
        document.Add(new iText.Layout.Element.Paragraph(content));
        document.Close();
    }
} 
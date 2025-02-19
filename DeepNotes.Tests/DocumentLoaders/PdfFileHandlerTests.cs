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
    public async Task LoadDocumentAsync_ValidPdf_ExtractsContent()
    {
        // Arrange
        var handler = new PdfFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Test PDF content", document.Content);
        Assert.Equal(_testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentAsync_WithMetadata_ExtractsMetadata()
    {
        // Arrange
        var handler = new PdfFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document.Metadata);
        Assert.Contains("PageCount", document.Metadata.Keys);
        Assert.Equal("1", document.Metadata["PageCount"]);
    }

    [Theory]
    [InlineData(".pdf")]
    public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var handler = new PdfFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.True(result);
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
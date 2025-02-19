using DeepNotes.DataLoaders.FileHandlers;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace DeepNotes.Tests.DocumentLoaders;

public class WordFileHandlerTests : IDisposable
{
    private readonly string _testFilePath;

    public WordFileHandlerTests()
    {
        _testFilePath = Path.GetTempFileName() + ".docx";
        CreateTestDocument(_testFilePath, "Test Word content");
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task LoadDocumentAsync_ValidWord_ExtractsContent()
    {
        // Arrange
        var handler = new WordFileHandler();

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Test Word content", document.Content);
        Assert.Equal(_testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentAsync_WithFormatting_ExtractsPlainText()
    {
        // Arrange
        var handler = new WordFileHandler();
        CreateFormattedDocument(_testFilePath);

        // Act
        var document = await handler.LoadDocumentAsync(_testFilePath);

        // Assert
        Assert.NotNull(document);
        Assert.Contains("Formatted content", document.Content);
        Assert.DoesNotContain("<b>", document.Content);
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData(".doc")]
    public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var handler = new WordFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.True(result);
    }

    private void CreateTestDocument(string filePath, string content)
    {
        using var document = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(new Paragraph(new Run(new Text(content)))));
    }

    private void CreateFormattedDocument(string filePath)
    {
        using var document = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        var run = new Run(
            new RunProperties(new Bold()),
            new Text("Formatted content")
        );
        mainPart.Document = new Document(new Body(new Paragraph(run)));
    }
} 
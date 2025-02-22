using System.Diagnostics.CodeAnalysis;
using DeepNotes.DataLoaders.FileHandlers;
using DeepNotes.DataLoaders.Utils;
using Xunit;

namespace DeepNotes.Tests.DocumentLoaders;

public class TextFileHandlerTests
{
    [Fact]
    [Experimental("SKEXP0055")]
    public async Task LoadDocumentAsync_ValidTextFile_LoadsContent()
    {
        // Arrange
        var handler = new TextFileHandler();
        var testFilePath = Path.GetTempFileName();
        var testContent = "Test content\nLine 2\nLine 3";
        await File.WriteAllTextAsync(testFilePath, testContent);

        try
        {
            // Act
            var document = await handler.LoadDocumentAsync(testFilePath);

            // Assert
            Assert.NotNull(document);
            Assert.Equal(testContent, document.Content);
            Assert.Equal(testFilePath, document.Source);
            Assert.Equal("File", document.SourceType);
            Assert.Contains("LineCount", document.Metadata.Keys);
            Assert.Equal("3", document.Metadata["LineCount"]);
        }
        finally
        {
            File.Delete(testFilePath);
        }
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".md")]
    [InlineData(".json")]
    [InlineData(".xml")]
    [InlineData(".csv")]
    public void CanHandle_SupportedExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var handler = new TextFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(".doc")]
    [InlineData(".pdf")]
    [InlineData(".unknown")]
    public void CanHandle_UnsupportedExtensions_ReturnsFalse(string extension)
    {
        // Arrange
        var handler = new TextFileHandler();

        // Act
        var result = handler.CanHandle(extension);

        // Assert
        Assert.False(result);
    }
}
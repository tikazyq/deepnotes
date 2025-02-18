namespace DeepNotes.Tests.DocumentLoaders;

using System.IO;
using DeepNotes.DataLoaders;
using DeepNotes.DataLoaders.FileHandlers;
using Xunit;
using Moq;

public class FileDocumentLoaderTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly string _testContent;

    public FileDocumentLoaderTests()
    {
        _testContent = "Test content\nSecond line";
        _testFilePath = Path.GetTempFileName();
        File.WriteAllText(_testFilePath, _testContent);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task LoadDocumentsAsync_TextFile_ReturnsDocumentWithMetadata()
    {
        // Arrange
        var loader = new FileDocumentLoader();

        // Act
        var documents = await loader.LoadDocumentsAsync(_testFilePath);
        var document = documents.First();

        // Assert
        Assert.Single(documents);
        Assert.Equal(_testContent, document.Content);
        Assert.Equal(_testFilePath, document.Source);
        Assert.Equal("File", document.SourceType);
        
        // Check basic metadata
        Assert.Equal(Path.GetExtension(_testFilePath), document.Metadata["FileExtension"]);
        Assert.Equal(Path.GetFileName(_testFilePath), document.Metadata["FileName"]);
        Assert.NotNull(document.Metadata["FileSize"]);
        
        // Check content-based metadata
        Assert.Equal(_testContent.Length.ToString(), document.Metadata["CharacterCount"]);
        Assert.Equal("3", document.Metadata["WordCount"]); // "Test content Second line" = 3 words
        Assert.Equal("2", document.Metadata["LineCount"]);
    }

    [Fact]
    public async Task LoadDocumentsAsync_UnsupportedFileType_ThrowsNotSupportedException()
    {
        // Arrange
        var loader = new FileDocumentLoader();
        var unsupportedFile = Path.GetTempFileName() + ".unsupported";
        File.WriteAllText(unsupportedFile, "content");

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<NotSupportedException>(() => 
                loader.LoadDocumentsAsync(unsupportedFile));
        }
        finally
        {
            File.Delete(unsupportedFile);
        }
    }

    [Fact]
    public async Task LoadDocumentsAsync_CustomHandler_UsesCustomHandler()
    {
        // Arrange
        var mockHandler = new Mock<IFileTypeHandler>();
        mockHandler.Setup(h => h.CanHandle(It.IsAny<string>())).Returns(true);
        mockHandler.Setup(h => h.ExtractTextAsync(It.IsAny<string>()))
            .ReturnsAsync("Custom content");
        mockHandler.Setup(h => h.ExtractMetadata(It.IsAny<string>()))
            .Returns(new Dictionary<string, string> { ["CustomMeta"] = "Value" });

        var loader = new FileDocumentLoader(new[] { mockHandler.Object });

        // Act
        var documents = await loader.LoadDocumentsAsync(_testFilePath);
        var document = documents.First();

        // Assert
        Assert.Equal("Custom content", document.Content);
        Assert.Equal("Value", document.Metadata["CustomMeta"]);
        mockHandler.Verify(h => h.ExtractTextAsync(_testFilePath), Times.Once);
    }
} 
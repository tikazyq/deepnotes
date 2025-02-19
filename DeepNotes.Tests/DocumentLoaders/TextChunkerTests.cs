using DeepNotes.DataLoaders.Utils;
using Xunit;

namespace DeepNotes.Tests.DocumentLoaders;

public class TextChunkerTests
{
    [Fact]
    public void CreateChunks_WithParagraphStrategy_SplitsCorrectly()
    {
        // Arrange
        var chunker = new TextChunker(
            strategy: TextChunker.ChunkStrategy.Paragraph,
            chunkSize: 100,
            overlap: 20
        );
        var text = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";

        // Act
        var chunks = chunker.CreateChunks(text);

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Equal("Paragraph 1", chunks[0].Content.Trim());
        Assert.Equal("Paragraph 2", chunks[1].Content.Trim());
        Assert.Equal("Paragraph 3", chunks[2].Content.Trim());
    }

    [Theory]
    [InlineData(TextChunker.ChunkStrategy.Character)]
    [InlineData(TextChunker.ChunkStrategy.Word)]
    [InlineData(TextChunker.ChunkStrategy.Sentence)]
    [InlineData(TextChunker.ChunkStrategy.Paragraph)]
    [InlineData(TextChunker.ChunkStrategy.MarkdownHeader)]
    public void CreateChunks_DifferentStrategies_ProducesChunks(TextChunker.ChunkStrategy strategy)
    {
        // Arrange
        var chunker = new TextChunker(strategy: strategy);
        var text = "# Header\nSentence one. Sentence two.\n\nNew paragraph.";

        // Act
        var chunks = chunker.CreateChunks(text);

        // Assert
        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk => Assert.NotEmpty(chunk.Content));
    }
} 
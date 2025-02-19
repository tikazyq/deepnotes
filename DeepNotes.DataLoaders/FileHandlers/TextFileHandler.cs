using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.Utils;

namespace DeepNotes.DataLoaders.FileHandlers;

public class TextFileHandler : IFileTypeHandler
{
    private readonly TextChunker _chunker;
    
    private static readonly string[] SupportedExtensions = { ".txt", ".md", ".json", ".xml", ".csv" };
    
    public TextFileHandler(TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
    }
    
    public TextChunker GetChunker()
    {
        return _chunker;
    }

    public bool CanHandle(string fileExtension)
    {
        return SupportedExtensions.Contains(fileExtension.ToLower());
    }

    public async Task<Document> LoadDocumentAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        var chunks = Chunker.CreateChunks(content);

        var metadata = new Dictionary<string, string>
        {
            ["Encoding"] = "UTF-8", // You could detect this
            ["LineCount"] = File.ReadAllLines(filePath).Length.ToString(),
            ["LastModified"] = new FileInfo(filePath).LastWriteTimeUtc.ToString("O"),
        };

        // Add chunking metadata
        foreach (var item in Chunker.GetChunkingMetadata(chunks.Count))
        {
            metadata[item.Key] = item.Value;
        }

        var document = new Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = metadata,
            Chunks = chunks
        };

        return document;
    }

    public TextChunker Chunker { get; }
}
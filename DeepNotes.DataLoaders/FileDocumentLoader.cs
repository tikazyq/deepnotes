using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.FileHandlers;
using DeepNotes.DataLoaders.Utils;

namespace DeepNotes.DataLoaders;

public class FileDocumentLoader : BaseDocumentLoader
{
    private readonly IEnumerable<IFileTypeHandler> _handlers;
    private readonly TextChunker _chunker;

    public FileDocumentLoader(IEnumerable<IFileTypeHandler>? handlers = null, TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
        _handlers = handlers ?? new IFileTypeHandler[]
        {
            new TextFileHandler(_chunker),
            new PdfFileHandler(_chunker),
            new WordFileHandler(_chunker),
            new ExcelFileHandler(_chunker),
            new PowerPointFileHandler(_chunker)
        };
    }

    public override string SourceType => "File";

    public override async Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier)
    {
        if (!File.Exists(sourceIdentifier))
        {
            throw new FileNotFoundException($"File not found: {sourceIdentifier}");
        }

        var extension = Path.GetExtension(sourceIdentifier);
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(extension));

        if (handler == null)
        {
            throw new NotSupportedException($"No handler found for file type: {extension}");
        }

        var document = await handler.LoadDocumentAsync(sourceIdentifier);

        // Add basic file metadata if not already present
        if (!document.Metadata.ContainsKey("FileExtension"))
            document.Metadata["FileExtension"] = extension;
        if (!document.Metadata.ContainsKey("FileName"))
            document.Metadata["FileName"] = Path.GetFileName(sourceIdentifier);
        if (!document.Metadata.ContainsKey("FileSize"))
            document.Metadata["FileSize"] = new FileInfo(sourceIdentifier).Length.ToString();

        // Add content-based metadata
        var contentMetadata = ExtractMetadata(document.Content);
        foreach (var (key, value) in contentMetadata)
        {
            if (!document.Metadata.ContainsKey(key))
                document.Metadata[key] = value;
        }

        return new[] { document };
    }

    protected override Dictionary<string, string> ExtractMetadata(string content)
    {
        var metadata = base.ExtractMetadata(content);

        // Add any content-based metadata extraction here
        metadata["CharacterCount"] = content.Length.ToString();
        metadata["WordCount"] = content.Split(new[] { ' ', '\n', '\r', '\t' },
            StringSplitOptions.RemoveEmptyEntries).Length.ToString();

        return metadata;
    }
}
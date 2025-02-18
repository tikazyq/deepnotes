namespace DeepNotes.DataLoaders;

using System.IO;
using DeepNotes.Core.Models;
using DeepNotes.DataLoaders.FileHandlers;

public class FileDocumentLoader : BaseDocumentLoader
{
    private readonly IEnumerable<IFileTypeHandler> _handlers;

    public FileDocumentLoader(IEnumerable<IFileTypeHandler>? handlers = null)
    {
        _handlers = handlers ?? new IFileTypeHandler[]
        {
            new TextFileHandler(),
            new PdfFileHandler(),
            new WordFileHandler(),
            new ExcelFileHandler(),
            new PowerPointFileHandler()
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

        var content = await handler.ExtractTextAsync(sourceIdentifier);
        var document = CreateDocument(content, sourceIdentifier);

        // Add basic file metadata
        document.Metadata["FileExtension"] = extension;
        document.Metadata["FileName"] = Path.GetFileName(sourceIdentifier);
        document.Metadata["FileSize"] = new FileInfo(sourceIdentifier).Length.ToString();

        // Add handler-specific metadata
        foreach (var (key, value) in handler.ExtractMetadata(sourceIdentifier))
        {
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
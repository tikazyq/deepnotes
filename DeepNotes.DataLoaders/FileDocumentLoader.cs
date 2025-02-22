using System.Diagnostics.CodeAnalysis;
using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.FileHandlers;
using DeepNotes.DataLoaders.Utils;

namespace DeepNotes.DataLoaders;

public class FileDocumentLoader : BaseDocumentLoader
{
    private readonly IEnumerable<IFileTypeHandler> _handlers;

    public FileDocumentLoader(IEnumerable<IFileTypeHandler>? handlers = null)
    {
        _handlers = handlers ??
        [
            new PdfFileHandler(),
            new WordFileHandler(),
            new ExcelFileHandler(),
            new PowerPointFileHandler(),
            new TextFileHandler()
        ];
    }

    public override string SourceType => "File";

    [Experimental("SKEXP0050")]
    public override async Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier)
    {
        if (File.Exists(sourceIdentifier))
        {
            var document = await LoadSingleDocumentAsync(sourceIdentifier);
            return [document];
        }
        
        if (Directory.Exists(sourceIdentifier))
        {
            // TODO: implement directory processing
            throw new NotImplementedException();
        }

        throw new FileNotFoundException();
    }

    [Experimental("SKEXP0050")]
    public async Task<Document> LoadSingleDocumentAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        var extension = Path.GetExtension(filePath);
        var handler = _handlers.FirstOrDefault(h => h.CanHandle(extension));

        if (handler == null)
        {
            throw new NotSupportedException($"No handler found for file type: {extension}");
        }

        var document = await handler.LoadDocumentAsync(filePath);

        // Add basic file metadata if not already present
        if (!document.Metadata.ContainsKey("FileExtension"))
            document.Metadata["FileExtension"] = extension;
        if (!document.Metadata.ContainsKey("FileName"))
            document.Metadata["FileName"] = Path.GetFileName(filePath);
        if (!document.Metadata.ContainsKey("FileSize"))
            document.Metadata["FileSize"] = new FileInfo(filePath).Length.ToString();

        // Add content-based metadata
        var contentMetadata = ExtractMetadata(document.Content);
        foreach (var (key, value) in contentMetadata)
        {
            if (!document.Metadata.ContainsKey(key))
                document.Metadata[key] = value;
        }
        
        // Split document content into chunks
        DocumentChunker.SplitDocumentContentToChunks(document);

        return document;
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
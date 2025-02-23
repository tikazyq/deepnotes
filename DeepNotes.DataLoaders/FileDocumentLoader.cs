using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
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
            var documents = new ConcurrentBag<Document>();
            var files = Directory.GetFiles(sourceIdentifier, "*.*", SearchOption.AllDirectories);

            // Configure parallel options - adjust MaxDegreeOfParallelism based on your needs
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            await Parallel.ForEachAsync(files, parallelOptions, async (file, token) =>
            {
                try
                {
                    var extension = Path.GetExtension(file);
                    if (_handlers.Any(h => h.CanHandle(extension)))
                    {
                        var document = await LoadSingleDocumentAsync(file);
                        documents.Add(document);
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle individual file processing errors
                    // Continue processing other files even if one fails
                    Console.WriteLine($"Error processing file {file}: {ex.Message}");
                }
            });

            return documents;
        }

        throw new FileNotFoundException($"No valid file or directory found at: {sourceIdentifier}");
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
using DeepNotes.Core.Models.Document;
using DeepNotes.Core.Interfaces;

namespace DeepNotes.DataLoaders;

public abstract class BaseDocumentLoader : IDocumentLoader
{
    public abstract string SourceType { get; }

    public abstract Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier);

    protected virtual Dictionary<string, string> ExtractMetadata(string content)
    {
        // Base implementation returns empty metadata
        // Derived classes can override to extract specific metadata
        return new Dictionary<string, string>();
    }

    protected Document CreateDocument(string content, string source)
    {
        return new Document
        {
            Content = content,
            Source = source,
            SourceType = SourceType,
            Metadata = ExtractMetadata(content)
        };
    }
}
using DeepNotes.Core.Models;

namespace DeepNotes.Core.Interfaces;

public interface IDocumentLoader
{
    Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier);
    string SourceType { get; }
} 
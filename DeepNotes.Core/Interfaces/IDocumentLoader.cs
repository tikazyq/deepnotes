using DeepNotes.Core.Models;
using DeepNotes.Core.Models.Document;

namespace DeepNotes.Core.Interfaces;

public interface IDocumentLoader
{
    Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier);
    string SourceType { get; }
} 
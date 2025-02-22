namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models.Document;

public interface IDocumentLoader
{
    Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier);
    string SourceType { get; }
}
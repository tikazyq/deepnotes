using DeepNotes.Core.Models.Document;

namespace DeepNotes.DataLoaders.FileHandlers;

public interface IFileTypeHandler
{
    bool CanHandle(string fileExtension);
    Task<Document> LoadDocumentAsync(string filePath);
} 
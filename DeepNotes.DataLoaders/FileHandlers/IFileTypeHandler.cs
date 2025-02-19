using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.Utils;

namespace DeepNotes.DataLoaders.FileHandlers;

public interface IFileTypeHandler
{
    bool CanHandle(string fileExtension);
    Task<Document> LoadDocumentAsync(string filePath);
    TextChunker GetChunker();
} 
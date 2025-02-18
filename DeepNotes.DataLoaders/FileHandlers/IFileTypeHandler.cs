namespace DeepNotes.DataLoaders.FileHandlers;

public interface IFileTypeHandler
{
    bool CanHandle(string fileExtension);
    Task<string> ExtractTextAsync(string filePath);
    Dictionary<string, string> ExtractMetadata(string filePath);
} 
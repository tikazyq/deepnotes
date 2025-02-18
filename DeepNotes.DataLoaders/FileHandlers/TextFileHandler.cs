namespace DeepNotes.DataLoaders.FileHandlers;

public class TextFileHandler : IFileTypeHandler
{
    private static readonly string[] SupportedExtensions = { ".txt", ".md", ".json", ".xml", ".csv" };

    public bool CanHandle(string fileExtension)
    {
        return SupportedExtensions.Contains(fileExtension.ToLower());
    }

    public async Task<string> ExtractTextAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    public Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return new Dictionary<string, string>
        {
            ["Encoding"] = "UTF-8", // You could detect this
            ["LineCount"] = File.ReadAllLines(filePath).Length.ToString(),
            ["LastModified"] = fileInfo.LastWriteTimeUtc.ToString("O")
        };
    }
} 
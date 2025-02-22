using System.Diagnostics.CodeAnalysis;
using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.Utils;
using Microsoft.SemanticKernel.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

public class TextFileHandler : IFileTypeHandler
{
    private static readonly string[] SupportedExtensions = { ".txt", ".md" };

    public bool CanHandle(string fileExtension)
    {
        return SupportedExtensions.Contains(fileExtension.ToLower());
    }

    [Experimental("SKEXP0055")]
    public async Task<Document> LoadDocumentAsync(string filePath)
    {
        var chunks = new List<string>();
        var content = await File.ReadAllTextAsync(filePath);

        var metadata = new Dictionary<string, string>
        {
            ["Encoding"] = "UTF-8", // You could detect this
            ["LineCount"] = (await File.ReadAllLinesAsync(filePath)).Length.ToString(),
            ["LastModified"] = new FileInfo(filePath).LastWriteTimeUtc.ToString("O"),
        };

        var document = new Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = metadata,
        };

        return document;
    }
}
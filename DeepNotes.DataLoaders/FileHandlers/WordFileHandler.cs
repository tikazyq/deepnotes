using System.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

public class WordFileHandler : OfficeFileHandlerBase
{
    protected override string[] SupportedExtensions => new[] { ".docx" };

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return WordprocessingDocument.Open(filePath, false);
    }

    public override async Task<string> ExtractTextAsync(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        var text = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            text.AppendLine(paragraph.InnerText);
        }

        return await Task.FromResult(text.ToString());
    }

    public override Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = base.ExtractMetadata(filePath);
        
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body != null)
        {
            metadata["ParagraphCount"] = body.Elements<Paragraph>().Count().ToString();
        }

        return metadata;
    }
} 
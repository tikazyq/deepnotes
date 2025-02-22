using DeepNotes.DataLoaders.Utils;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeepNotes.DataLoaders.FileHandlers;

public class WordFileHandler : OfficeFileHandlerBase
{
    protected override string[] SupportedExtensions => new[] { ".docx", ".doc" };

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return WordprocessingDocument.Open(filePath, false);
    }

    public override async Task<Core.Models.Document.Document> LoadDocumentAsync(string filePath)
    {
        using var doc = WordprocessingDocument.Open(filePath, false);
        var body = doc.MainDocumentPart?.Document.Body;

        var text = new StringBuilder();
        if (body != null)
        {
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }
        }

        var content = text.ToString();
        var metadata = ExtractBaseMetadata(doc);

        if (body != null)
        {
            metadata["ParagraphCount"] = body.Elements<Paragraph>().Count().ToString();
        }

        var document = new Core.Models.Document.Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = metadata,
        };

        return document;
    }
}
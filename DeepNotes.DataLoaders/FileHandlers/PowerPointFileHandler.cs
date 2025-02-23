using System.Text;
using DeepNotes.DataLoaders.Utils;
using DeepNotes.Core.Models.Document;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace DeepNotes.DataLoaders.FileHandlers;

public class PowerPointFileHandler : OfficeFileHandlerBase
{
    protected override string[] SupportedExtensions => new[] { ".pptx", ".ppt" };

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return PresentationDocument.Open(filePath, false);
    }

    public override async Task<Document> LoadDocumentAsync(string filePath)
    {
        var text = new StringBuilder();
        var metadata = new Dictionary<string, string>();

        await Task.Run(() =>
        {
            using var doc = PresentationDocument.Open(filePath, false);
            metadata = ExtractBaseMetadata(doc);

            var presentationPart = doc.PresentationPart;

            if (presentationPart == null)
            {
                throw new InvalidOperationException("Invalid PowerPoint file");
            }

            int slideNumber = 1;
            foreach (var slidePart in presentationPart.SlideParts)
            {
                text.AppendLine($"Slide {slideNumber++}");
                text.AppendLine("-------------------");

                // Extract text from all shapes in the slide
                text.AppendLine(slidePart.Slide.InnerText);
                text.AppendLine();
            }

            metadata["SlideCount"] = (slideNumber - 1).ToString();
        });

        return new Document
        {
            Content = text.ToString().Trim(),
            Source = filePath,
            SourceType = "File",
            Metadata = metadata
        };
    }
}
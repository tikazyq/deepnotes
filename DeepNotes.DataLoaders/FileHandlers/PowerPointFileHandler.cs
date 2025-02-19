using System.Text;
using DeepNotes.DataLoaders.Utils;
using DeepNotes.Core.Models.Document;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

namespace DeepNotes.DataLoaders.FileHandlers;

public class PowerPointFileHandler : OfficeFileHandlerBase
{
    private readonly TextChunker _chunker;

    protected override string[] SupportedExtensions => new[] { ".pptx" };

    public PowerPointFileHandler(TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
    }

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return PresentationDocument.Open(filePath, false);
    }

    public override async Task<Document> LoadDocumentAsync(string filePath)
    {
        using var doc = PresentationDocument.Open(filePath, false);
        var presentationPart = doc.PresentationPart;
        var text = new StringBuilder();
        var metadata = ExtractBaseMetadata(doc);

        if (presentationPart?.Presentation.SlideIdList != null)
        {
            int slideNumber = 1;
            var slideCount = 0;
            foreach (var slideId in presentationPart.Presentation.SlideIdList.ChildElements.OfType<SlideId>())
            {
                var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                text.AppendLine($"Slide {slideNumber}");
                text.AppendLine("-------------------");

                var paragraphs = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>();
                foreach (var paragraph in paragraphs)
                {
                    text.AppendLine(paragraph.InnerText);
                }

                text.AppendLine();
                slideNumber++;
                slideCount++;
            }

            metadata["SlideCount"] = slideCount.ToString();
        }

        var document = new Document
        {
            Content = text.ToString(),
            Source = filePath,
            SourceType = "File",
            Metadata = metadata
        };

        return await Task.FromResult(document);
    }
}
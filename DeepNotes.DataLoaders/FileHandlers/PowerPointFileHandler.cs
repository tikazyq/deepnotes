using System.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

public class PowerPointFileHandler : OfficeFileHandlerBase
{
    protected override string[] SupportedExtensions => new[] { ".pptx" };

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return PresentationDocument.Open(filePath, false);
    }

    public override async Task<string> ExtractTextAsync(string filePath)
    {
        using var doc = PresentationDocument.Open(filePath, false);
        var presentationPart = doc.PresentationPart;
        if (presentationPart == null) return string.Empty;

        var text = new StringBuilder();
        var slideIds = presentationPart.Presentation.SlideIdList;
        
        if (slideIds != null)
        {
            int slideNumber = 1;
            foreach (var slideId in slideIds.ChildElements.OfType<SlideId>())
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
            }
        }

        return await Task.FromResult(text.ToString());
    }

    public override Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = base.ExtractMetadata(filePath);
        
        using var doc = PresentationDocument.Open(filePath, false);
        var presentationPart = doc.PresentationPart;
        if (presentationPart?.Presentation.SlideIdList != null)
        {
            metadata["SlideCount"] = presentationPart.Presentation.SlideIdList
                .ChildElements.Count.ToString();
        }

        return metadata;
    }
} 
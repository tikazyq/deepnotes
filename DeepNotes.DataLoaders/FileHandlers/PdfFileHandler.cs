using System.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

using DeepNotes.Core.Models.Document;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DeepNotes.DataLoaders.Utils;

public class PdfFileHandler : IFileTypeHandler
{
    public bool CanHandle(string fileExtension)
    {
        return fileExtension.ToLower() == ".pdf";
    }

    public async Task<Document> LoadDocumentAsync(string filePath)
    {
        using var pdfReader = new PdfReader(filePath);
        using var pdfDocument = new PdfDocument(pdfReader);
        var text = new StringBuilder();

        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            var page = pdfDocument.GetPage(i);
            var strategy = new LocationTextExtractionStrategy();
            var currentText = PdfTextExtractor.GetTextFromPage(page, strategy);
            text.AppendLine(currentText);
            text.AppendLine();
        }

        var content = text.ToString();

        var document = new Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = ExtractMetadata(pdfDocument),
        };

        return document;
    }

    private Dictionary<string, string> ExtractMetadata(PdfDocument pdfDocument)
    {
        var metadata = new Dictionary<string, string>
        {
            ["PageCount"] = pdfDocument.GetNumberOfPages().ToString(),
            ["PdfVersion"] = pdfDocument.GetPdfVersion().ToString()
        };

        var info = pdfDocument.GetDocumentInfo();
        if (info != null)
        {
            if (!string.IsNullOrEmpty(info.GetTitle())) metadata["Title"] = info.GetTitle();
            if (!string.IsNullOrEmpty(info.GetAuthor())) metadata["Author"] = info.GetAuthor();
            if (!string.IsNullOrEmpty(info.GetCreator())) metadata["Creator"] = info.GetCreator();
            if (!string.IsNullOrEmpty(info.GetProducer())) metadata["Producer"] = info.GetProducer();
        }

        return metadata;
    }
}
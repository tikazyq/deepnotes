using System.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

public class PdfFileHandler : IFileTypeHandler
{
    public bool CanHandle(string fileExtension)
    {
        return fileExtension.ToLower() == ".pdf";
    }

    public async Task<string> ExtractTextAsync(string filePath)
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
        }

        return await Task.FromResult(text.ToString());
    }

    public Dictionary<string, string> ExtractMetadata(string filePath)
    {
        using var pdfReader = new PdfReader(filePath);
        using var pdfDocument = new PdfDocument(pdfReader);
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
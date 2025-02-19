using DeepNotes.DataLoaders.Utils;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DeepNotes.DataLoaders.FileHandlers;

public class WordFileHandler : OfficeFileHandlerBase
{
    private readonly TextChunker _chunker;
    
    protected override string[] SupportedExtensions => new[] { ".docx" };
    
    public WordFileHandler(TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
    }

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return WordprocessingDocument.Open(filePath, false);
    }

    public override async Task<Core.Models.Document.Document> LoadDocumentAsync(string filePath)
    {
        var chunker = GetChunker();
        
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
        var chunks = chunker.CreateChunks(content);
        var metadata = ExtractBaseMetadata(doc);

        if (body != null)
        {
            metadata["ParagraphCount"] = body.Elements<Paragraph>().Count().ToString();
        }

        // Add chunking metadata
        foreach (var item in chunker.GetChunkingMetadata(chunks.Count))
        {
            metadata[item.Key] = item.Value;
        }

        var document = new Core.Models.Document.Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = metadata,
            Chunks = chunks
        };

        return document;
    }
}
using System.Text;
using DeepNotes.Core.Models.Document;
using DeepNotes.DataLoaders.Utils;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DeepNotes.DataLoaders.FileHandlers;

public class ExcelFileHandler : OfficeFileHandlerBase
{
    private readonly TextChunker _chunker;

    protected override string[] SupportedExtensions => new[] { ".xlsx" };

    public ExcelFileHandler(TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
    }

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return SpreadsheetDocument.Open(filePath, false);
    }

    public override async Task<Document> LoadDocumentAsync(string filePath)
    {
        var chunker = GetChunker();

        using var doc = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = doc.WorkbookPart;
        var text = new StringBuilder();
        var metadata = ExtractBaseMetadata(doc);

        if (workbookPart != null)
        {
            var sheets = workbookPart.Workbook.Descendants<Sheet>();
            metadata["SheetCount"] = sheets.Count().ToString();

            foreach (var sheet in sheets)
            {
                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

                text.AppendLine($"Sheet: {sheet.Name}");
                text.AppendLine("-------------------");

                var cells = worksheetPart.Worksheet.Descendants<Cell>();
                foreach (var cell in cells)
                {
                    string value = GetCellValue(cell, sharedStringTable);
                    if (!string.IsNullOrEmpty(value))
                    {
                        text.AppendLine(value);
                    }
                }

                text.AppendLine();
            }
        }

        var content = text.ToString();
        var chunks = chunker.CreateChunks(content);

        // Add chunking metadata
        foreach (var item in chunker.GetChunkingMetadata(chunks.Count))
        {
            metadata[item.Key] = item.Value;
        }

        var document = new Document
        {
            Content = content,
            Source = filePath,
            SourceType = "File",
            Metadata = metadata,
            Chunks = chunks
        };

        return document;
    }

    private static string GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
    {
        if (cell.CellValue == null) return string.Empty;

        string value = cell.CellValue.Text;
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && sharedStringTable != null)
        {
            return sharedStringTable.ElementAt(int.Parse(value)).InnerText;
        }

        return value;
    }
}
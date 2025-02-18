using System.Text;

namespace DeepNotes.DataLoaders.FileHandlers;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class ExcelFileHandler : OfficeFileHandlerBase
{
    protected override string[] SupportedExtensions => new[] { ".xlsx" };

    protected override OpenXmlPackage OpenDocument(string filePath)
    {
        return SpreadsheetDocument.Open(filePath, false);
    }

    public override async Task<string> ExtractTextAsync(string filePath)
    {
        using var doc = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart == null) return string.Empty;

        var text = new StringBuilder();
        var sheets = workbookPart.Workbook.Descendants<Sheet>();

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

        return await Task.FromResult(text.ToString());
    }

    public override Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = base.ExtractMetadata(filePath);
        
        using var doc = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart != null)
        {
            var sheets = workbookPart.Workbook.Descendants<Sheet>();
            metadata["SheetCount"] = sheets.Count().ToString();
        }

        return metadata;
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
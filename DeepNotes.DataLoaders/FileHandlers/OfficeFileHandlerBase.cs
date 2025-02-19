using DeepNotes.DataLoaders.Utils;

namespace DeepNotes.DataLoaders.FileHandlers;

using DeepNotes.Core.Models.Document;
using DocumentFormat.OpenXml.Packaging;

public abstract class OfficeFileHandlerBase : IFileTypeHandler
{
    private readonly TextChunker _chunker;
    
    
    protected OfficeFileHandlerBase(TextChunker? chunker = null)
    {
        _chunker = chunker ?? new TextChunker();
    }

    protected abstract string[] SupportedExtensions { get; }
   
    public TextChunker GetChunker()
    {
        return _chunker;
    } 
    
    public bool CanHandle(string fileExtension)
    {
        return SupportedExtensions.Contains(fileExtension.ToLower());
    }

    public abstract Task<Document> LoadDocumentAsync(string filePath);

    protected Dictionary<string, string> ExtractBaseMetadata(OpenXmlPackage doc)
    {
        var metadata = new Dictionary<string, string>();
        var props = doc.PackageProperties;
        if (props.Created.HasValue) metadata["Created"] = props.Created.Value.ToString("O");
        if (props.Modified.HasValue) metadata["Modified"] = props.Modified.Value.ToString("O");
        if (!string.IsNullOrEmpty(props.Creator)) metadata["Creator"] = props.Creator;
        if (!string.IsNullOrEmpty(props.Title)) metadata["Title"] = props.Title;
        return metadata;
    }

    protected abstract OpenXmlPackage OpenDocument(string filePath);
} 
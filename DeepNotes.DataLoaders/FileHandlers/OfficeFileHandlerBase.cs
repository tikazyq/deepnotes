namespace DeepNotes.DataLoaders.FileHandlers;

using DocumentFormat.OpenXml.Packaging;

public abstract class OfficeFileHandlerBase : IFileTypeHandler
{
    protected abstract string[] SupportedExtensions { get; }
    
    public bool CanHandle(string fileExtension)
    {
        return SupportedExtensions.Contains(fileExtension.ToLower());
    }

    public abstract Task<string> ExtractTextAsync(string filePath);

    public virtual Dictionary<string, string> ExtractMetadata(string filePath)
    {
        var metadata = new Dictionary<string, string>();
        using var doc = OpenDocument(filePath);
        
        var props = doc.PackageProperties;
        if (props != null)
        {
            if (props.Created.HasValue) metadata["Created"] = props.Created.Value.ToString("O");
            if (props.Modified.HasValue) metadata["Modified"] = props.Modified.Value.ToString("O");
            if (!string.IsNullOrEmpty(props.Creator)) metadata["Creator"] = props.Creator;
            if (!string.IsNullOrEmpty(props.Title)) metadata["Title"] = props.Title;
        }

        return metadata;
    }

    protected abstract OpenXmlPackage OpenDocument(string filePath);
} 
namespace DeepNotes.API.Models;

public class DocumentUploadRequest
{
    public string Source { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class DocumentResponse
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DocumentListResponse
{
    public IEnumerable<DocumentResponse> Documents { get; set; } = Array.Empty<DocumentResponse>();
    public int Total { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
} 
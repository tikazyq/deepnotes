namespace DeepNotes.Core.Models;

public class DocumentMetadata
{
    public Guid Id { get; set; }
    public string Source { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public string? ErrorMessage { get; set; }
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
} 
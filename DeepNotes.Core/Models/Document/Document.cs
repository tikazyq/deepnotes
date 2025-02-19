namespace DeepNotes.Core.Models.Document;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public IReadOnlyList<DocumentChunk> Chunks { get; set; } = Array.Empty<DocumentChunk>();
}
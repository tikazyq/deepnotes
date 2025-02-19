namespace DeepNotes.Core.Models.Document;

public class DocumentChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int? Index { get; set; } = null;
    public string Content { get; set; } = string.Empty;
}
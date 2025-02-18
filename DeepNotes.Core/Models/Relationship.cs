namespace DeepNotes.Core.Models;

public class Relationship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceEntityId { get; set; }
    public Guid TargetEntityId { get; set; }
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 
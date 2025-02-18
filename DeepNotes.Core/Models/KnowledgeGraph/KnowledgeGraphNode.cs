namespace DeepNotes.Core.Models.KnowledgeGraph;

public class KnowledgeGraphNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public HashSet<string> Sources { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
} 
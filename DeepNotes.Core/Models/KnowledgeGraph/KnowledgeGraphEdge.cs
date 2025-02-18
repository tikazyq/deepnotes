namespace DeepNotes.Core.Models.KnowledgeGraph;

public class KnowledgeGraphEdge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceNodeId { get; set; }
    public Guid TargetNodeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public HashSet<string> Sources { get; set; } = new();
    public double Weight { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 
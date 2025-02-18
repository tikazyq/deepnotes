namespace DeepNotes.Core.Models.KnowledgeGraph;

public class KnowledgeGraphPattern
{
    public Dictionary<string, KnowledgeGraphNode> Nodes { get; set; } = new();
    public Dictionary<string, KnowledgeGraphEdge> Edges { get; set; } = new();
} 
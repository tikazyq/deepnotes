namespace DeepNotes.Core.Models.KnowledgeGraph;

public class KnowledgeGraphPath
{
    public List<KnowledgeGraphNode> Nodes { get; set; } = new();
    public List<KnowledgeGraphEdge> Edges { get; set; } = new();
    public int Length => Edges.Count;
    public double TotalWeight => Edges.Sum(e => e.Weight);
} 
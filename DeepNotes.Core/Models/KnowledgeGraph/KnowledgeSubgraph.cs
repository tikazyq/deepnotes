namespace DeepNotes.Core.Models.KnowledgeGraph;

public class KnowledgeSubgraph
{
    public HashSet<KnowledgeGraphNode> Nodes { get; set; } = new();
    public HashSet<KnowledgeGraphEdge> Edges { get; set; } = new();
    public KnowledgeGraphNode CentralNode { get; set; } = null!;
    public int Depth { get; set; }
} 
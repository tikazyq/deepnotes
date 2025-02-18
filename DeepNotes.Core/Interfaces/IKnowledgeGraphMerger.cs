namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models.KnowledgeGraph;

public interface IKnowledgeGraphMerger
{
    Task<KnowledgeGraphNode> MergeNodesAsync(KnowledgeGraphNode node1, KnowledgeGraphNode node2);
    Task<bool> ShouldMergeNodesAsync(KnowledgeGraphNode node1, KnowledgeGraphNode node2);
    Task<double> CalculateNodeSimilarityAsync(KnowledgeGraphNode node1, KnowledgeGraphNode node2);
} 
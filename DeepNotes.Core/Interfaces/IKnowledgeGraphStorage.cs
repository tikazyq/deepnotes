namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models.KnowledgeGraph;

public interface IKnowledgeGraphStorage
{
    Task<KnowledgeGraphNode> CreateNodeAsync(KnowledgeGraphNode node);
    Task<KnowledgeGraphNode?> GetNodeAsync(Guid id);
    Task<KnowledgeGraphNode> UpdateNodeAsync(KnowledgeGraphNode node);
    Task DeleteNodeAsync(Guid id);
    
    Task<KnowledgeGraphEdge> CreateEdgeAsync(KnowledgeGraphEdge edge);
    Task<KnowledgeGraphEdge?> GetEdgeAsync(Guid id);
    Task<KnowledgeGraphEdge> UpdateEdgeAsync(KnowledgeGraphEdge edge);
    Task DeleteEdgeAsync(Guid id);
    
    Task<IEnumerable<KnowledgeGraphNode>> FindNodesAsync(string labelPattern, string type);
    Task<IEnumerable<KnowledgeGraphEdge>> FindEdgesAsync(Guid sourceNodeId, string? type = null);
    Task<IEnumerable<KnowledgeGraphNode>> GetConnectedNodesAsync(Guid nodeId, string? edgeType = null, string? direction = null);

    // Advanced querying capabilities
    Task<IEnumerable<KnowledgeGraphPath>> FindPathsAsync(
        Guid sourceId, 
        Guid targetId, 
        int? maxDepth = null, 
        string[]? edgeTypes = null);

    Task<KnowledgeSubgraph> ExtractSubgraphAsync(
        Guid centralNodeId, 
        int depth, 
        string[]? nodeTypes = null, 
        string[]? edgeTypes = null);

    Task<IEnumerable<KnowledgeGraphPattern>> FindPatternsAsync(
        string patternQuery, 
        IDictionary<string, object> parameters);
} 
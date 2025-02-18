namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models;
using DeepNotes.Core.Models.KnowledgeGraph;

public interface IKnowledgeGraphService
{
    Task<KnowledgeSubgraph> ProcessDocumentsAsync(IEnumerable<Document> documents);
    Task<KnowledgeSubgraph> MergeSubgraphsAsync(IEnumerable<KnowledgeSubgraph> subgraphs);
    Task<IEnumerable<KnowledgeGraphNode>> SearchNodesAsync(string query, string? type = null);
    Task<IEnumerable<KnowledgeGraphPath>> FindConnectionsAsync(string sourceLabel, string targetLabel, int? maxDepth = null);
    Task<KnowledgeSubgraph> GetNodeContextAsync(string nodeLabel, int depth = 2);
    Task<IEnumerable<KnowledgeGraphPattern>> FindPatternsAsync(string patternDescription);
} 
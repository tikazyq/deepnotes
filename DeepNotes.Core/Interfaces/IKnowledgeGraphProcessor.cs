namespace DeepNotes.Core.Interfaces;

using DeepNotes.Core.Models;
using DeepNotes.Core.Models.KnowledgeGraph;

public interface IKnowledgeGraphProcessor
{
    Task<IEnumerable<KnowledgeGraphNode>> ExtractNodesAsync(Document document);
    Task<IEnumerable<KnowledgeGraphEdge>> ExtractEdgesAsync(Document document, IEnumerable<KnowledgeGraphNode> nodes);
    Task<(IEnumerable<KnowledgeGraphNode> Nodes, IEnumerable<KnowledgeGraphEdge> Edges)> ProcessDocumentAsync(Document document);
} 
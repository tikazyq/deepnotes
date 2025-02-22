using System.Diagnostics.CodeAnalysis;
using DeepNotes.Core.Models.Document;
using Microsoft.SemanticKernel.Text;

namespace DeepNotes.DataLoaders.Utils;

public static class DocumentChunker
{
    [Experimental("SKEXP0050")]
    public static void SplitDocumentContentToChunks(Document document, int chunkSize = 2000,
        int chunkOverlap = 200)
    {
        var chunkStrings = TextChunker.SplitPlainTextParagraphs(
            document.Content.Split("\n").ToList(),
            chunkSize,
            chunkOverlap
        );

        document.Chunks = Array.Empty<DocumentChunk>();
        for (var i = 0; i < chunkStrings.Count; i++)
        {
            document.Chunks.Append(new DocumentChunk()
            {
                Index = i,
                Content = chunkStrings[i]
            });
        }
    }
}
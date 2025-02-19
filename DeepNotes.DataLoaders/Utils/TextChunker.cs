namespace DeepNotes.DataLoaders.Utils;

using DeepNotes.Core.Models.Document;
using System.Text;
using System.Text.RegularExpressions;

public class TextChunker
{
    public enum ChunkStrategy
    {
        Character,
        Word,
        Sentence,
        Paragraph,
        MarkdownHeader
    }

    private readonly ChunkStrategy _strategy;
    private readonly int _chunkSize;
    private readonly int _overlap;
    private readonly string _separator;

    public TextChunker(
        ChunkStrategy strategy = ChunkStrategy.Paragraph,
        int chunkSize = 1000,
        int overlap = 200,
        string? separator = null)
    {
        _strategy = strategy;
        _chunkSize = chunkSize;
        _overlap = overlap;
        _separator = separator ?? Environment.NewLine;
    }

    public List<DocumentChunk> CreateChunks(string content)
    {
        var splits = SplitText(content);
        return CreateChunksFromSplits(splits);
    }

    private List<string> SplitText(string text)
    {
        return _strategy switch
        {
            ChunkStrategy.Character => text.Select(c => c.ToString()).ToList(),
            ChunkStrategy.Word => Regex.Split(text, @"\s+").Where(w => !string.IsNullOrWhiteSpace(w)).ToList(),
            ChunkStrategy.Sentence => SplitIntoSentences(text),
            ChunkStrategy.Paragraph => SplitIntoParagraphs(text),
            ChunkStrategy.MarkdownHeader => SplitByMarkdownHeaders(text),
            _ => throw new ArgumentException($"Unknown chunking strategy: {_strategy}")
        };
    }

    private List<DocumentChunk> CreateChunksFromSplits(List<string> splits)
    {
        var chunks = new List<DocumentChunk>();
        var currentChunk = new StringBuilder();
        var chunkIndex = 0;
        var splitQueue = new Queue<string>();

        foreach (var split in splits)
        {
            if (currentChunk.Length + split.Length > _chunkSize && currentChunk.Length > 0)
            {
                // Add current chunk
                chunks.Add(new DocumentChunk
                {
                    Index = chunkIndex++,
                    Content = currentChunk.ToString().Trim()
                });

                // Handle overlap
                currentChunk.Clear();
                if (_overlap > 0)
                {
                    var overlapContent = string.Join(_separator, 
                        splitQueue.TakeLast(CalculateOverlapUnits(splitQueue, _overlap)));
                    currentChunk.Append(overlapContent);
                }

                splitQueue.Clear();
            }

            currentChunk.Append(split).Append(_separator);
            splitQueue.Enqueue(split);
        }

        // Add final chunk if there's content
        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Index = chunkIndex,
                Content = currentChunk.ToString().Trim()
            });
        }

        return chunks;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        // More sophisticated sentence splitting
        return Regex.Split(text, @"(?<=[.!?])\s+")
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .Select(s => s.Trim())
                   .ToList();
    }

    private static List<string> SplitIntoParagraphs(string text)
    {
        return Regex.Split(text, @"\n\s*\n")
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(p => p.Trim())
                   .ToList();
    }

    private static List<string> SplitByMarkdownHeaders(string text)
    {
        // Split by markdown headers (##, ###, etc.)
        return Regex.Split(text, @"(?=^#{1,6}\s)", RegexOptions.Multiline)
                   .Where(s => !string.IsNullOrWhiteSpace(s))
                   .Select(s => s.Trim())
                   .ToList();
    }

    private static int CalculateOverlapUnits(Queue<string> splits, int desiredOverlap)
    {
        int totalLength = 0;
        int unitCount = 0;

        foreach (var split in splits.Reverse())
        {
            totalLength += split.Length;
            unitCount++;

            if (totalLength >= desiredOverlap)
                break;
        }

        return Math.Max(1, unitCount);
    }

    public Dictionary<string, string> GetChunkingMetadata(int chunkCount)
    {
        return new Dictionary<string, string>
        {
            ["ChunkCount"] = chunkCount.ToString(),
            ["ChunkSize"] = _chunkSize.ToString(),
            ["ChunkOverlap"] = _overlap.ToString(),
            ["ChunkStrategy"] = _strategy.ToString()
        };
    }
} 
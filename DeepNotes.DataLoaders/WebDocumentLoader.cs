using DeepNotes.Core.Models.Document;

namespace DeepNotes.DataLoaders;

public class WebDocumentLoader : BaseDocumentLoader
{
    private readonly HttpClient _httpClient;

    public WebDocumentLoader(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public override string SourceType => "Web";

    public override async Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier)
    {
        if (!Uri.TryCreate(sourceIdentifier, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL provided", nameof(sourceIdentifier));
        }

        var response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // Create document with web-specific metadata
        var document = CreateDocument(content, sourceIdentifier);
        document.Metadata["ContentType"] = response.Content.Headers.ContentType?.ToString() ?? "";
        document.Metadata["LastModified"] = response.Content.Headers.LastModified?.ToString() ?? "";

        return new[] { document };
    }

    protected override Dictionary<string, string> ExtractMetadata(string content)
    {
        var metadata = base.ExtractMetadata(content);

        // Add web-specific metadata extraction here if needed
        // For example, you could extract meta tags from HTML content

        return metadata;
    }
}
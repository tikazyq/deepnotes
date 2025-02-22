using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using DeepNotes.DataLoaders.Utils;
using HtmlAgilityPack;
using Polly;
using Polly.Retry;
using SmartReader;
using AngleSharp;
using AngleSharp.Html.Parser;
using Document = DeepNotes.Core.Models.Document.Document;

namespace DeepNotes.DataLoaders;

public class WebDocumentLoader : BaseDocumentLoader
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentHashSet<string> _visitedUrls;
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
    private readonly SemaphoreSlim _visitedLock;
    private readonly IHtmlParser _htmlParser;
    private readonly IBrowsingContext _browsingContext;

    private readonly int _timeout;
    private readonly int _concurrency;
    private readonly int _retries;
    private readonly double _backoffFactor;

    public WebDocumentLoader(
        HttpClient? httpClient = null,
        int timeout = 10,
        int concurrency = 10,
        int retries = 3,
        double backoffFactor = 1.0)
    {
        _httpClient = httpClient ?? new HttpClient();
        _visitedUrls = new ConcurrentHashSet<string>();
        _visitedLock = new SemaphoreSlim(1, 1);
        _timeout = timeout;
        _concurrency = concurrency;
        _retries = retries;
        _backoffFactor = backoffFactor;

        // Initialize AngleSharp
        var config = Configuration.Default;
        _browsingContext = BrowsingContext.New(config);
        _htmlParser = _browsingContext.GetService<IHtmlParser>();

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(_retries, retryAttempt =>
                    TimeSpan.FromSeconds(_backoffFactor * Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Retrying in {timeSpan.TotalSeconds}s (attempt {retryCount}/{_retries})");
                });

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; DeepNotes/1.0)");
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeout);
    }

    public override string SourceType => "Web";

    private bool IsValidUrl(string url, string urlPattern) =>
        url.StartsWith(urlPattern, StringComparison.OrdinalIgnoreCase);

    private async Task<string> ExtractMainContent(string html, string url)
    {
        var content = "";

        // Parse HTML with AngleSharp
        var htmlDocument = await _htmlParser.ParseDocumentAsync(html);

        // Try Trafilatura first
        try
        {
            content = await ExtractMainContentWithTrafilatura(html);
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Trafilatura extraction failed: {ex.Message}");
        }

        // Fallback to SmartReader
        try
        {
            var reader = new Reader(url, htmlDocument);
            var article = await reader.GetArticleAsync();

            if (article.IsReadable)
            {
                content = CleanText(article.TextContent);
                if (!string.IsNullOrWhiteSpace(content))
                    return content;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SmartReader extraction failed: {ex.Message}");
        }

        // Fallback to AngleSharp with improved logic
        try
        {
            // Remove non-content elements
            var elementsToRemove = htmlDocument.QuerySelectorAll("script, style, nav, header, footer, aside");
            foreach (var element in elementsToRemove)
            {
                element?.Remove();
            }

            // Try common content containers in priority order
            var selectors = new[]
            {
                "article",
                "main",
                ".main-content",
                ".article-body",
                "#content",
                "#main",
                "[role='main']",
                ".content",
                ".post-content"
            };

            foreach (var selector in selectors)
            {
                var elements = htmlDocument.QuerySelectorAll(selector);
                if (elements.Length > 0)
                {
                    var text = string.Join("\n", elements.Select(e => e.TextContent.Trim()));
                    if (text.Length > 200) // Minimum content length threshold
                    {
                        return CleanText(text);
                    }
                }
            }

            // Fallback to body
            var body = htmlDocument.Body;
            if (body != null)
            {
                return CleanText(body.TextContent);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AngleSharp extraction failed: {ex.Message}");
        }

        return content;
    }

    private async Task<string> ExtractMainContentWithTrafilatura(string html)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = "-m trafilatura",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true, // 重定向错误输出
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8 // 设置输出编码为 UTF-8
            };

            var process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            // 4. 将 html 文件内容写入 trafilatura 进程的标准输入
            await using (var sw = process.StandardInput)
            {
                await sw.WriteAsync(html);
            }

            // 5. 读取 trafilatura 进程的标准输出 (提取的正文) 和标准错误
            var extractedText = await process.StandardOutput.ReadToEndAsync();
            var errorOutput = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(); // 等待 trafilatura 进程结束

            if (!string.IsNullOrEmpty(errorOutput))
            {
                Console.WriteLine($"Trafilatura 错误输出: {errorOutput}");
                // 可以选择抛出异常或返回错误信息
            }

            return extractedText.Trim(); // 返回提取的正文 (去除首尾空白)
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Trafilatura 提取正文失败: {ex.Message}");
            return "";
        }
    }

    private async Task<HttpResponseMessage?> FetchUrlAsync(string url)
    {
        try
        {
            var response = await _retryPolicy.ExecuteAsync(async () =>
            {
                var result = await _httpClient.GetAsync(url);
                if (result.IsSuccessStatusCode)
                    return result;

                if ((int)result.StatusCode >= 500)
                    throw new HttpRequestException($"Server error: {result.StatusCode}");

                return result;
            });

            return response.IsSuccessStatusCode ? response : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to fetch {url}: {ex.Message}");
            return null;
        }
    }

    private async Task<List<string>> CrawlAsync(List<string> startUrls, string urlPattern)
    {
        var urlQueue = new ConcurrentQueue<string>(startUrls);
        var processedUrls = new ConcurrentBag<string>();

        async Task ProcessUrlBatchAsync()
        {
            while (urlQueue.TryDequeue(out var url))
            {
                if (!_visitedUrls.Contains(url))
                {
                    await _visitedLock.WaitAsync();
                    try
                    {
                        if (_visitedUrls.Add(url))
                        {
                            processedUrls.Add(url);
                            var newUrls = await ProcessUrlAsync(url, urlPattern);
                            foreach (var newUrl in newUrls)
                            {
                                urlQueue.Enqueue(newUrl);
                            }
                        }
                    }
                    finally
                    {
                        _visitedLock.Release();
                    }
                }
            }
        }

        var tasks = Enumerable.Range(0, _concurrency)
            .Select(_ => ProcessUrlBatchAsync())
            .ToList();

        await Task.WhenAll(tasks);
        return processedUrls.ToList();
    }

    private async Task<List<string>> ProcessUrlAsync(string url, string urlPattern)
    {
        var newUrls = new List<string>();
        try
        {
            var response = await FetchUrlAsync(url);
            if (response != null)
            {
                var html = await response.Content.ReadAsStringAsync();
                var htmlDocument = await _htmlParser.ParseDocumentAsync(html);

                var links = htmlDocument.QuerySelectorAll("a[href]");
                foreach (var link in links)
                {
                    var href = link.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        var absoluteUrl = new Uri(new Uri(url), href).AbsoluteUri;
                        var cleanUrl = Regex.Replace(absoluteUrl, @"#.*|\?.*", "");
                        if (IsValidUrl(cleanUrl, urlPattern) && !_visitedUrls.Contains(cleanUrl))
                        {
                            newUrls.Add(cleanUrl);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing {url}: {ex.Message}");
        }

        return newUrls;
    }

    public override async Task<IEnumerable<Document>> LoadDocumentsAsync(string sourceIdentifier)
    {
        if (!Uri.TryCreate(sourceIdentifier, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL provided", nameof(sourceIdentifier));
        }

        var startUrls = new List<string> { sourceIdentifier };
        var urlPattern = $"{uri.Scheme}://{uri.Host}";

        Console.WriteLine("Starting URL discovery...");
        var urls = await CrawlAsync(startUrls, urlPattern);
        Console.WriteLine($"Found {urls.Count} URLs to process");

        var tasks = urls.Select(async url =>
        {
            try
            {
                var response = await FetchUrlAsync(url);
                if (response != null)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var content = await ExtractMainContent(html, url);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        var metadata = ExtractMetadata(html);
                        metadata["ContentType"] = response.Content.Headers.ContentType?.ToString() ?? "";
                        metadata["LastModified"] = response.Content.Headers.LastModified?.ToString() ?? "";
                        metadata["FileSize"] = content.Length.ToString();

                        return new Document
                        {
                            Content = content,
                            Source = url,
                            SourceType = SourceType,
                            Metadata = metadata,
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {url}: {ex.Message}");
            }

            return null;
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(doc => doc != null)!;
    }

    protected override Dictionary<string, string> ExtractMetadata(string html)
    {
        var metadata = base.ExtractMetadata(html);
        var htmlDocument = _htmlParser.ParseDocument(html);

        // Extract meta tags
        var metaTags = htmlDocument.QuerySelectorAll("meta");
        foreach (var meta in metaTags)
        {
            var name = meta.GetAttribute("name");
            var property = meta.GetAttribute("property");
            var content = meta.GetAttribute("content");

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
            {
                metadata[name] = content;
            }
            else if (!string.IsNullOrEmpty(property) && !string.IsNullOrEmpty(content))
            {
                // Handle OpenGraph and other property-based meta tags
                var key = property.Replace(":", "_");
                metadata[key] = content;
            }
        }

        // Extract title
        var titleNode = htmlDocument.QuerySelector("title");
        if (titleNode != null)
        {
            metadata["Title"] = titleNode.TextContent.Trim();
        }

        return metadata;
    }

    private string CleanHtml(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return CleanText(doc.DocumentNode.InnerText);
    }

    private string CleanText(string text)
    {
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }
}

// Helper class for thread-safe set operations
public class ConcurrentHashSet<T>
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    public bool Add(T item) => _dictionary.TryAdd(item, 0);
    public bool Contains(T item) => _dictionary.ContainsKey(item);
    public bool Remove(T item) => _dictionary.TryRemove(item, out _);
}
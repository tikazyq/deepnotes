namespace DeepNotes.Tests.DocumentLoaders;

using System.Net;
using System.Net.Http;
using DeepNotes.DataLoaders;
using Moq;
using Moq.Protected;
using Xunit;
using HtmlAgilityPack;

public class WebDocumentLoaderTests
{
    [Fact]
    public async Task LoadDocument_ValidWebPage_ExtractsContent()
    {
        // Arrange
        var htmlContent = @"
            <!DOCTYPE html>
            <html>
                <head>
                    <title>Test Page</title>
                    <meta name='description' content='Page description'>
                </head>
                <body>
                    <h1>Main Heading</h1>
                    <article>
                        <p>Important content paragraph</p>
                        <div class='navigation'>Menu items</div>
                    </article>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var httpClient = new HttpClient(handler.Object);
        var loader = new WebDocumentLoader(httpClient);
        var url = "https://example.com/article";

        // Act
        var result = await loader.LoadDocumentAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Main Heading", result.Content);
        Assert.Contains("Important content paragraph", result.Content);
        Assert.Equal(url, result.Source);
        Assert.Equal("Web", result.SourceType);
        Assert.Equal("Test Page", result.Metadata["Title"]);
        Assert.Equal("Page description", result.Metadata["Description"]);
    }

    [Fact]
    public async Task LoadDocument_ArticleWithMetadata_ExtractsMetadata()
    {
        // Arrange
        var htmlContent = @"
            <!DOCTYPE html>
            <html>
                <head>
                    <meta property='og:title' content='Article Title'>
                    <meta property='og:description' content='Article description'>
                    <meta property='article:author' content='John Doe'>
                    <meta property='article:published_time' content='2024-03-15'>
                </head>
                <body>
                    <article>Article content</article>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var result = await loader.LoadDocumentAsync("https://example.com/article");

        // Assert
        Assert.Equal("Article Title", result.Metadata["OgTitle"]);
        Assert.Equal("Article description", result.Metadata["OgDescription"]);
        Assert.Equal("John Doe", result.Metadata["Author"]);
        Assert.Equal("2024-03-15", result.Metadata["PublishedDate"]);
    }

    [Fact]
    public async Task LoadDocument_WithPaywall_HandlesPaywallDetection()
    {
        // Arrange
        var htmlContent = @"
            <html>
                <body>
                    <div class='paywall-overlay'>
                        Subscribe to continue reading
                    </div>
                    <article class='premium-content'>
                        Hidden content
                    </article>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var result = await loader.LoadDocumentAsync("https://example.com/premium-article");

        // Assert
        Assert.True(result.Metadata.ContainsKey("HasPaywall"));
        Assert.Equal("True", result.Metadata["HasPaywall"]);
    }

    [Fact]
    public async Task LoadDocument_NonExistentPage_ThrowsException()
    {
        // Arrange
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not Found")
            });

        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            loader.LoadDocumentAsync("https://example.com/not-found"));
    }

    [Fact]
    public async Task LoadDocument_WithJavaScript_ExtractsStaticContent()
    {
        // Arrange
        var htmlContent = @"
            <html>
                <body>
                    <div id='static-content'>Visible content</div>
                    <script>
                        document.getElementById('dynamic').innerHTML = 'Dynamic content';
                    </script>
                    <div id='dynamic'></div>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var result = await loader.LoadDocumentAsync("https://example.com/js-page");

        // Assert
        Assert.Contains("Visible content", result.Content);
        Assert.DoesNotContain("Dynamic content", result.Content);
    }

    [Fact]
    public async Task LoadDocument_WithComments_FiltersOutComments()
    {
        // Arrange
        var htmlContent = @"
            <html>
                <body>
                    <article>Main content</article>
                    <div class='comments-section'>
                        <div class='comment'>User comment 1</div>
                        <div class='comment'>User comment 2</div>
                    </div>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var result = await loader.LoadDocumentAsync("https://example.com/article-with-comments");

        // Assert
        Assert.Contains("Main content", result.Content);
        Assert.DoesNotContain("User comment 1", result.Content);
        Assert.DoesNotContain("User comment 2", result.Content);
    }

    private Mock<HttpMessageHandler> SetupMockHttpMessageHandler(string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });

        return handler;
    }
} 
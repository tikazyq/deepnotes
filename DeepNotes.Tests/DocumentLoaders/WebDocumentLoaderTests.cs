using System.Net;
using DeepNotes.DataLoaders;
using Moq;
using Moq.Protected;
using Xunit;

namespace DeepNotes.Tests.DocumentLoaders;

public class WebDocumentLoaderTests
{
    private const string LongArticleHtml = @"
        <!DOCTYPE html>
        <html>
            <head>
                <title>The Future of Artificial Intelligence</title>
                <meta name='description' content='An in-depth analysis of AI trends and future implications'>
                <meta name='author' content='Dr. Jane Smith'>
                <meta name='keywords' content='AI, machine learning, future technology, deep learning'>
                <meta property='og:title' content='The Future of AI: A Comprehensive Analysis'>
                <meta property='og:description' content='Exploring the future implications of artificial intelligence'>
                <meta property='article:author' content='Dr. Jane Smith'>
                <meta property='article:published_time' content='2024-03-15'>
            </head>
            <body>
                <header>
                    <nav>
                        <ul>
                            <li><a href='/home'>Home</a></li>
                            <li><a href='/about'>About</a></li>
                        </ul>
                    </nav>
                </header>
                <main>
                    <article class='main-content'>
                        <h1>The Future of Artificial Intelligence</h1>
                        
                        <p class='article-meta'>By Dr. Jane Smith | Published: March 15, 2024</p>
                        
                        <p class='article-intro'>Artificial Intelligence has become an integral part of our daily lives, 
                        transforming how we work, communicate, and solve problems. This article explores the future 
                        implications of AI technology and its potential impact on society.</p>

                        <h2>Current State of AI Technology</h2>
                        <p>The field of artificial intelligence has witnessed remarkable progress in recent years. 
                        Machine learning algorithms have achieved unprecedented accuracy in tasks ranging from image 
                        recognition to natural language processing. Deep learning models can now generate human-like 
                        text, create artistic images, and even compose music.</p>

                        <p>Recent developments in neural networks have led to systems that can:</p>
                        <ul>
                            <li>Process and understand natural language with near-human accuracy</li>
                            <li>Generate creative content across multiple mediums</li>
                            <li>Solve complex problems in scientific research</li>
                            <li>Assist in medical diagnosis and treatment planning</li>
                        </ul>

                        <h2>Future Implications</h2>
                        <p>As AI technology continues to evolve, we can expect significant changes in various sectors:</p>
                        
                        <h3>Healthcare</h3>
                        <p>AI-powered systems will revolutionize healthcare through improved diagnostic accuracy, 
                        personalized treatment plans, and drug discovery acceleration. Machine learning algorithms 
                        will analyze vast amounts of medical data to identify patterns and insights that humans 
                        might miss.</p>

                        <h3>Transportation</h3>
                        <p>Autonomous vehicles will become increasingly common, potentially reducing accidents and 
                        improving traffic flow. AI will optimize routing and logistics, making transportation more 
                        efficient and environmentally friendly.</p>

                        <h3>Education</h3>
                        <p>Personalized learning experiences will become the norm, with AI tutors adapting to each 
                        student's unique learning style and pace. Virtual and augmented reality will create immersive 
                        educational experiences.</p>

                        <h2>Challenges and Considerations</h2>
                        <p>While the potential benefits are significant, we must address several challenges:</p>
                        <ul>
                            <li>Ethical considerations in AI decision-making</li>
                            <li>Privacy concerns and data protection</li>
                            <li>Impact on employment and workforce transition</li>
                            <li>Ensuring AI systems remain transparent and accountable</li>
                        </ul>

                        <h2>Conclusion</h2>
                        <p>The future of artificial intelligence holds immense promise for improving our lives and 
                        solving complex global challenges. However, careful consideration of ethical implications 
                        and responsible development practices will be crucial for ensuring these technologies 
                        benefit society as a whole.</p>
                    </article>

                    <div class='comments-section'>
                        <h3>Comments</h3>
                        <div class='comment'>
                            <p>Great article! The healthcare implications are particularly interesting.</p>
                            <p class='comment-meta'>Posted by: User123 | 2 hours ago</p>
                        </div>
                        <div class='comment'>
                            <p>I'm concerned about the employment impact. How will we handle job displacement?</p>
                            <p class='comment-meta'>Posted by: TechWorker | 1 hour ago</p>
                        </div>
                    </div>
                </main>

                <footer>
                    <p>Copyright © 2024 | Privacy Policy | Terms of Service</p>
                </footer>

                <script>
                    // Dynamic content loading script
                    document.addEventListener('DOMContentLoaded', function() {
                        loadComments();
                    });
                </script>
            </body>
        </html>";

    [Fact]
    public async Task LoadDocumentsAsync_ValidWebPage_ExtractsContent()
    {
        // Arrange
        var handler = SetupMockHttpMessageHandler(LongArticleHtml);
        var httpClient = new HttpClient(handler.Object);
        var loader = new WebDocumentLoader(httpClient);
        var url = "https://example.com/";

        // Act
        var results = await loader.LoadDocumentsAsync(url);
        var result = results.First();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("The Future of Artificial Intelligence", result.Content);
        Assert.Contains("Current State of AI Technology", result.Content);
        Assert.Contains("The field of artificial intelligence has witnessed remarkable progress", result.Content);
        Assert.Contains("Conclusion", result.Content);
        Assert.DoesNotContain("Copyright © 2024", result.Content); // Footer should be removed
        Assert.DoesNotContain("Posted by: User123", result.Content); // Comments should be removed
        Assert.StartsWith(url, result.Source);
        Assert.Equal("Web", result.SourceType);
    }

    [Fact]
    public async Task LoadDocumentsAsync_ArticleWithMetadata_ExtractsMetadata()
    {
        // Arrange
        var handler = SetupMockHttpMessageHandler(LongArticleHtml);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/article");
        var result = results.First();

        // Assert
        Assert.Equal("The Future of AI: A Comprehensive Analysis", result.Metadata["og_title"]);
        Assert.Equal("Exploring the future implications of artificial intelligence", result.Metadata["og_description"]);
        Assert.Equal("Dr. Jane Smith", result.Metadata["article_author"]);
        Assert.Equal("2024-03-15", result.Metadata["article_published_time"]);
        Assert.Equal("AI, machine learning, future technology, deep learning", result.Metadata["keywords"]);
        Assert.Equal("Dr. Jane Smith", result.Metadata["author"]);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithPaywall_DetectsPaywall()
    {
        // Arrange
        var paywallHtml = @"
            <!DOCTYPE html>
            <html>
                <head>
                    <title>Premium AI Analysis</title>
                    <meta name='description' content='Premium content about AI trends'>
                </head>
                <body>
                    <article class='article-preview'>
                        <h1>The Future of AI: Advanced Analysis</h1>
                        <p>Artificial intelligence is revolutionizing industries across the globe. 
                        Recent developments in machine learning have led to breakthrough applications in healthcare,
                        finance, and transportation...</p>
                        
                        <div class='paywall-overlay'>
                            <h2>Subscribe to Continue Reading</h2>
                            <p>Get unlimited access to premium content and in-depth analysis.</p>
                            <ul>
                                <li>Exclusive expert insights</li>
                                <li>Detailed technical analysis</li>
                                <li>Industry case studies</li>
                            </ul>
                            <button class='subscribe-button'>Subscribe Now</button>
                        </div>

                        <div class='premium-content'>
                            <h2>Detailed Analysis</h2>
                            <p>This premium section contains in-depth analysis of AI trends, including:
                            statistical data, expert interviews, and future predictions...</p>
                            <!-- More premium content -->
                        </div>
                    </article>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(paywallHtml);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/premium-article");
        var result = results.First();

        // Assert
        Assert.Contains("Artificial intelligence is revolutionizing industries", result.Content);
    }

    [Fact]
    public async Task LoadDocumentsAsync_NonExistentPage_ReturnsEmptyCollection()
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

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/not-found");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithJavaScript_ExtractsStaticContent()
    {
        // Arrange
        var htmlContent = @"
            <html>
                <body>
                    <div id='static-content'>This is the visible content of the page.</div>
                    <script>
                        document.getElementById('dynamic').innerHTML = 'This content is dynamically generated and should not be extracted.';
                    </script>
                    <div id='dynamic'></div>
                    <div id='more-static-content'>More visible content.</div>
                </body>
            </html>";

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/js-page");
        var result = results.First();

        // Assert
        Assert.Contains("This is the visible content of the page.", result.Content);
        Assert.Contains("More visible content.", result.Content);
        Assert.DoesNotContain("This content is dynamically generated and should not be extracted.", result.Content);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithComments_FiltersOutComments()
    {
        // Arrange
        var htmlContent = LongArticleHtml;

        var handler = SetupMockHttpMessageHandler(htmlContent);
        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/article-with-comments");
        var result = results.First();

        // Assert
        Assert.Contains("The field of artificial intelligence has witnessed remarkable progress in recent years.", result.Content);
        Assert.DoesNotContain("Great article! The healthcare implications are particularly interesting.", result.Content);
        Assert.DoesNotContain("I'm concerned about the employment impact. How will we handle job displacement?", result.Content);
    }

    [Fact]
    public async Task LoadDocumentsAsync_WithMultipleUrls_CrawlsAndProcessesAll()
    {
        // Arrange
        var mainPageHtml = @"
            <html>
                <body>
                    <a href='/page1'>Page 1</a>
                    <a href='/page2'>Page 2</a>
                    <article>Main page content</article>
                </body>
            </html>";

        var page1Html = @"
            <html>
                <body>
                    <article>Page 1 content</article>
                </body>
            </html>";

        var page2Html = @"
            <html>
                <body>
                    <article>Page 2 content</article>
                </body>
            </html>";

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("main")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mainPageHtml)
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("page1")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(page1Html)
            });

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("page2")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(page2Html)
            });

        var loader = new WebDocumentLoader(new HttpClient(handler.Object));

        // Act
        var results = await loader.LoadDocumentsAsync("https://example.com/main");

        // Assert
        Assert.Equal(3, results.Count());
        Assert.Contains(results, d => d.Content.Contains("Main page content"));
        Assert.Contains(results, d => d.Content.Contains("Page 1 content"));
        Assert.Contains(results, d => d.Content.Contains("Page 2 content"));
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
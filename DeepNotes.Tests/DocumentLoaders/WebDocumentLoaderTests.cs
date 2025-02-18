namespace DeepNotes.Tests.DocumentLoaders;

using System.Net;
using System.Net.Http;
using DeepNotes.DataLoaders;
using Moq;
using Moq.Protected;
using Xunit;

public class WebDocumentLoaderTests
{
    [Fact]
    public async Task LoadDocumentsAsync_ValidUrl_ReturnsDocument()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var expectedContent = "Test content";
        var url = "https://example.com";

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var loader = new WebDocumentLoader(httpClient);

        // Act
        var documents = await loader.LoadDocumentsAsync(url);
        var document = documents.First();

        // Assert
        Assert.Single(documents);
        Assert.Equal(expectedContent, document.Content);
        Assert.Equal(url, document.Source);
        Assert.Equal("Web", document.SourceType);
    }

    [Fact]
    public async Task LoadDocumentsAsync_InvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var loader = new WebDocumentLoader();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            loader.LoadDocumentsAsync("not-a-url"));
    }
} 
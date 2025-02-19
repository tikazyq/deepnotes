using System.Text.Json;
using System.Text.Json.Serialization;
using DeepNotes.LLM;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Xunit;

namespace DeepNotes.Tests.LLM;

public class LLMServiceFixture : IDisposable
{
    public LLMConfig Config { get; }
    public Mock<IChatCompletionService> ChatServiceMock { get; }
    public LLMService Service { get; }

    public LLMServiceFixture()
    {
        Config = new LLMConfig
        {
            Model = "test-model",
            Language = "english",
            Endpoint = "https://test.endpoint",
            ApiKey = "test-key"
        };

        ChatServiceMock = new Mock<IChatCompletionService>();

        Service = new LLMService(Config, ChatServiceMock.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    public static LLMServiceFixture Create()
    {
        return new LLMServiceFixture();
    }
}

public class LLMServiceTests : IClassFixture<LLMServiceFixture>
{
    [Fact]
    public async Task GenerateAsync_ValidPrompt_ReturnsResponse()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        var expectedContent = "Test response";
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, expectedContent)
        };

        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await fixture.Service.GenerateAsync("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedContent, result.Content);
    }

    [Fact]
    public async Task GenerateAsync_EmptyResponse_ThrowsException()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.GenerateAsync("Test prompt"));
    }

    [Fact]
    public async Task GenerateAsync_NullContent_ThrowsException()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, string.Empty)
        };

        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.GenerateAsync("Test prompt"));
    }

    public class TestModel
    {
        [JsonPropertyName("value")] public string Value { get; set; } = "";
    }

    [Fact]
    public async Task GenerateStructuredAsync_ValidJson_ReturnsDeserializedModel()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        var testModel = new TestModel { Value = "test" };
        var jsonResponse = JsonSerializer.Serialize(testModel);
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, jsonResponse)
        };

        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ModelInstance);
        Assert.Equal("test", result.ModelInstance.Value);
        Assert.Equal(jsonResponse, result.Content);
    }

    [Fact]
    public async Task GenerateStructuredAsync_InvalidJson_AttemptsCorrection()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        var invalidJson = "{invalid_json}";
        var validJson = """{"value": "corrected"}""";
        var mockResponses = new Queue<List<ChatMessageContent>>(new[]
        {
            // First response - invalid JSON
            new List<ChatMessageContent> { new(AuthorRole.Assistant, invalidJson) },
            // Correction response - valid JSON
            new List<ChatMessageContent> { new(AuthorRole.Assistant, "```json\n" + validJson + "\n```") }
        });

        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => mockResponses.Dequeue());

        // Act
        var result = await fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ModelInstance);
        Assert.Equal("corrected", result.ModelInstance.Value);
        Assert.Equal(validJson, result.Content);
    }

    [Fact]
    public async Task GenerateStructuredAsync_MaxRetriesExceeded_ThrowsException()
    {
        var fixture = LLMServiceFixture.Create();
        
        // Arrange
        var invalidJson = "{invalid_json}";
        fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { new(AuthorRole.Assistant, invalidJson) });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt"));

        Assert.Contains("Max retries exceeded", exception.Message);

        // Verify correction was attempted 4 times (initial + 3 retries)
        fixture.ChatServiceMock.Verify(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }
}
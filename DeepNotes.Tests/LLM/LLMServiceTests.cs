using System.Text.Json;
using Azure.AI.OpenAI;
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
        
        Service = new LLMService(Config);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

public class LLMServiceTests : IClassFixture<LLMServiceFixture>
{
    private readonly LLMServiceFixture _fixture;

    public LLMServiceTests(LLMServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GenerateAsync_ValidPrompt_ReturnsResponse()
    {
        // Arrange
        var expectedContent = "Test response";
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, expectedContent)
        };
        
        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _fixture.Service.GenerateAsync("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedContent, result.Content);
    }

    [Fact]
    public async Task GenerateAsync_EmptyResponse_ThrowsException()
    {
        // Arrange
        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.Service.GenerateAsync("Test prompt"));
    }

    [Fact]
    public async Task GenerateAsync_NullContent_ThrowsException()
    {
        // Arrange
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, string.Empty)
        };

        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.Service.GenerateAsync("Test prompt"));
    }

    public class TestModel
    {
        public string Value { get; set; } = "";
    }

    [Fact]
    public async Task GenerateStructuredAsync_ValidJson_ReturnsDeserializedModel()
    {
        // Arrange
        var testModel = new TestModel { Value = "test" };
        var jsonResponse = JsonSerializer.Serialize(testModel);
        var mockResponse = new List<ChatMessageContent>
        {
            new(AuthorRole.Assistant, jsonResponse)
        };

        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ModelInstance);
        Assert.Equal("test", result.ModelInstance.Value);
        Assert.Equal(jsonResponse, result.Content);
    }

    [Fact]
    public async Task GenerateStructuredAsync_InvalidJson_AttemptsCorrection()
    {
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

        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(mockResponses.Dequeue()));

        // Act
        var result = await _fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ModelInstance);
        Assert.Equal("corrected", result.ModelInstance.Value);
        Assert.Equal(validJson, result.Content);
    }

    [Fact]
    public async Task GenerateStructuredAsync_MaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var invalidJson = "{invalid_json}";
        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { new(AuthorRole.Assistant, invalidJson) });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.Service.GenerateStructuredAsync<TestModel>("Test prompt"));

        Assert.Contains("Max retries exceeded", exception.Message);

        // Verify correction was attempted 3 times (initial + 3 retries)
        _fixture.ChatServiceMock.Verify(s => s.GetChatMessageContentsAsync(
                It.IsAny<ChatHistory>(),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(4));
    }

    [Fact]
    public async Task GenerateAsync_ChineseLanguage_UsesChineseSystemPrompt()
    {
        _fixture.ChatServiceMock.Setup(s => s.GetChatMessageContentsAsync(
                It.Is<ChatHistory>(h => h[0].Content.Contains("使用中文回答")),
                It.IsAny<PromptExecutionSettings>(),
                It.IsAny<Kernel>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessageContent> { new(AuthorRole.Assistant, "测试回复") });

        // Act
        var result = await _fixture.Service.GenerateAsync("Test prompt");

        // Assert
        Assert.Equal("测试回复", result.Content);
    }
}
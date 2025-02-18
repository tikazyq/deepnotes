using System.ClientModel;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Polly;
using Polly.Retry;
using OpenAI;
using OpenAI.Chat;
using ChatMessageContent = OpenAI.Chat.ChatMessageContent;

namespace DeepNotes.LLM;

public record LLMResponse(
    string Content,
    string Provider,
    string Model,
    int InputTokens,
    int OutputTokens
);

public class LLMConfig
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4";
    public string? BaseUrl { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiVersion { get; set; }
    public string Language { get; set; } = "english";
}

public class LLMService
{
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly LLMConfig _config;
    private readonly Kernel _kernel;

    public LLMService(LLMConfig config)
    {
        _config = config;
        _kernel = KernelWrapper.CreateKernel(config);

        _retryPolicy = Policy
            .Handle<Exception>(ex => ex is HttpRequestException or KernelException)
            .WaitAndRetryAsync(5, attempt =>
                TimeSpan.FromSeconds(Math.Pow(1.5, attempt)));
    }

    public async Task<LLMResponse> GenerateAsync(
        string prompt,
        Type? responseModel = null)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var chat = _kernel.GetRequiredService<IChatCompletionService>();
            var history = PrepareChatHistory(prompt);

            var result = await chat.GetChatMessageContentAsync(
                history
            );

            return ProcessResponse(result, responseModel);
        });
    }

    private ChatHistory PrepareChatHistory(string prompt)
    {
        var systemMessage = _config.Language.ToLower() switch
        {
            "chinese" => "你是一个全能助手，能够理解所有语言，但请始终使用中文回答。你的回答必须使用中文，并保持简洁准确。",
            _ =>
                $"You are a helpful assistant who understands all languages. Please respond in {_config.Language} using clear and natural expressions."
        };

        return new ChatHistory
        {
            new(AuthorRole.System, systemMessage),
            new(AuthorRole.User, prompt)
        };
    }

    private LLMResponse ProcessResponse(
        ChatMessageContent response,
        Type? responseModel)
    {
        var content = response.Content ?? string.Empty;

        if (responseModel != null)
        {
            try
            {
                return ValidateAndWrapResponse(content, responseModel);
            }
            catch (JsonException ex)
            {
                return HandleValidationError(content, ex, responseModel);
            }
        }

        return new LLMResponse(
            content,
            _config.Provider,
            _config.Model,
            response.Metadata?.Usage?.PromptTokens ?? 0,
            response.Metadata?.Usage?.CompletionTokens ?? 0
        );
    }

    private LLMResponse ValidateAndWrapResponse(
        string content,
        Type responseModel)
    {
        var modelInstance = JsonSerializer.Deserialize(
            content,
            responseModel);

        return new LLMResponse(
            content,
            _config.Provider,
            _config.Model,
            0, 0
        );
    }

    private LLMResponse HandleValidationError(
        string invalidJson,
        JsonException error,
        Type responseModel)
    {
        // Implement self-correction logic similar to Python version
        var correctionPrompt = $"""
                                JSON validation failed. Error: {error.Message}
                                Invalid JSON: {invalidJson}
                                Please correct the JSON to match the {responseModel.Name} schema.
                                Respond ONLY with the corrected JSON.
                                """;

        var correctedResponse = GenerateAsync(correctionPrompt).Result;
        return ValidateAndWrapResponse(correctedResponse.Content, responseModel);
    }
}

// Kernel configuration helper
public static class KernelWrapper
{
    public static Kernel CreateKernel(LLMConfig config)
    {
        var builder = Kernel.CreateBuilder();

        var options = new OpenAIClientOptions
        {
            Endpoint = config.BaseUrl != null ? new Uri(config.BaseUrl) : null
        };

        var client = new ChatClient(config.Model, new ApiKeyCredential(config.ApiKey!), options);

        builder.Services.AddSingleton<IChatCompletionService>(
            new OpenAIChatCompletionService(config.Model, client));

        return builder.Build();
    }
}
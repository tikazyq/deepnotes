using System.Text.Json;
using Azure;
using DeepNotes.LLM.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAIClient = Azure.AI.OpenAI.OpenAIClient;

namespace DeepNotes.LLM;

public class LLMConfig
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "gpt-4o";

    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiVersion { get; set; }
    public string Language { get; set; } = "english";
}

public class LLMService
{
    private readonly LLMConfig _config;
    private readonly IChatCompletionService _service;

    public LLMService(LLMConfig config)
    {
        _config = config;
        _service = CreateChatCompletionService();
    }

    public async Task<LLMResponse> GenerateAsync(
        string prompt)
    {
        var history = PrepareChatHistory(prompt);

        var response = await _service.GetChatMessageContentsAsync(history);

        return ValidateResponse(response);
    }

    public async Task<StructuredLLMResponse<T>> GenerateStructuredAsync<T>(string prompt)
    {
        var history = PrepareChatHistory(prompt);

        var response = await _service.GetChatMessageContentsAsync(history);

        return await ValidateAndWrapResponseAsync<T>(prompt, response);
    }

    private IChatCompletionService CreateChatCompletionService()
    {
        return _config.Provider.ToLower() switch
        {
            "openai" => new OpenAIChatCompletionService(_config.Model,
                new OpenAIClient(new Uri(_config.Endpoint ?? string.Empty),
                    new AzureKeyCredential(_config.ApiKey ?? string.Empty))),
            _ => throw new NotSupportedException($"Provider {_config.Provider} is not supported.")
        };
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

    private LLMResponse ValidateResponse(IReadOnlyList<ChatMessageContent>? response)
    {
        if (response == null)
        {
            throw new InvalidOperationException("No response from the model.");
        }

        if (response.Count == 0)
        {
            throw new InvalidOperationException("Response count is zero.");
        }

        if (response[0].Content == null)
        {
            throw new InvalidOperationException("Response content is null.");
        }

        return new LLMResponse
        {
            Content = response[0].Content!
        };
    }

    private async Task<StructuredLLMResponse<T>> ValidateAndWrapResponseAsync<T>(string prompt,
        IReadOnlyList<ChatMessageContent> response)
    {
        T? modelInstance;

        var llmResponse = ValidateResponse(response);

        try
        {
            modelInstance = JsonSerializer.Deserialize<T>(llmResponse.Content);
        }
        catch (JsonException error)
        {
            return await CorrectAndValidateResponse<T>(prompt, llmResponse.Content, error);
        }

        if (modelInstance == null)
        {
            throw new InvalidOperationException("Failed to deserialize response content.");
        }

        return new StructuredLLMResponse<T> { Content = llmResponse.Content, ModelInstance = modelInstance };
    }

    private async Task<StructuredLLMResponse<T>> CorrectAndValidateResponse<T>(
        string originalPrompt,
        string invalidJson,
        JsonException error,
        int maxRetries = 3,
        List<string>? previousErrors = null)
    {
        previousErrors ??= [];
        previousErrors.Add($"Error: {error.Message}\nInvalid JSON: {invalidJson}");

        if (maxRetries <= 0)
        {
            throw new InvalidOperationException($"Max retries exceeded. Errors:\n{string.Join("\n", previousErrors)}");
        }

        var errorHistory = string.Join("\n", previousErrors.Select((e, i) => $"Attempt {i + 1}: {e}"));

        var correctionPrompt = $"""
                                JSON validation failed multiple times. Error history:
                                {errorHistory}

                                Original prompt: {originalPrompt}

                                Please carefully correct the JSON to match the required schema.
                                Respond ONLY with the corrected JSON between ```json markers.
                                """;

        var correctedResponse = await GenerateAsync(correctionPrompt);
        var correctedContent = ExtractJsonFromMarkdown(correctedResponse.Content);

        try
        {
            var modelInstance = JsonSerializer.Deserialize<T>(correctedContent) ??
                                throw new InvalidOperationException("Deserialized null value");

            return new StructuredLLMResponse<T>
            {
                Content = correctedContent,
                ModelInstance = modelInstance
            };
        }
        catch (JsonException ex)
        {
            return await CorrectAndValidateResponse<T>(
                originalPrompt,
                correctedContent,
                ex,
                maxRetries - 1,
                previousErrors
            );
        }
    }

    private string ExtractJsonFromMarkdown(string markdownText)
    {
        var jsonStart = markdownText.IndexOf("```json", StringComparison.Ordinal);
        if (jsonStart == -1) return markdownText;

        jsonStart += 7; // Skip ```json\n
        var jsonEnd = markdownText.LastIndexOf("```", StringComparison.Ordinal);
        return jsonEnd > jsonStart ? markdownText[jsonStart..jsonEnd].Trim() : markdownText;
    }
}
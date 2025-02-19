namespace DeepNotes.LLM.Models;

public class StructuredLLMResponse<T> : LLMResponse
{
    public required T ModelInstance { get; init; }
}
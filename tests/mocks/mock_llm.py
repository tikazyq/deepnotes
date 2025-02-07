from deepnotes.llm import LLMResponse, LLMWrapper


class MockLLMWrapper(LLMWrapper):
    def generate(self, prompt: str) -> LLMResponse:
        return LLMResponse(
            content=f"Mock response for: {prompt[:50]}...",
            model="mock-model",
            input_tokens=100,
            output_tokens=50,
        )

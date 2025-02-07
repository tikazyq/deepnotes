from typing import Literal, Optional

from openai import OpenAI
from openai.types.chat import ChatCompletion
from pydantic import BaseModel

# Supported providers with OpenAI-compatible APIs
OpenAICompatibleProviders = Literal[
    "openai", "together", "perplexity", "deepseek", "azure", "ollama", "local"
]


class LLMResponse(BaseModel):
    content: str
    model: str
    input_tokens: int
    output_tokens: int


class LLMConfig(BaseModel):
    provider: OpenAICompatibleProviders
    model_name: str
    base_url: Optional[str] = None
    api_key: Optional[str] = None
    api_version: Optional[str] = None  # For Azure
    temperature: float = 0.7
    max_tokens: int = 1024


class LLMWrapper:
    def __init__(self, config: LLMConfig):
        self.config = config
        self.client = self._create_client()

    def _create_client(self):
        """Initialize client for OpenAI-compatible providers"""
        client_params = {
            "api_key": self.config.api_key,
        }

        # Set base URL for compatible providers
        if self.config.provider != "openai":
            client_params["base_url"] = self.config.base_url

        # Special handling for Azure
        if self.config.provider == "azure":
            client_params["api_version"] = self.config.api_version

        return OpenAI(**client_params)

    def generate(self, prompt: str) -> LLMResponse:
        """Generate response with cost tracking"""
        response: ChatCompletion = self.client.chat.completions.create(
            model=self.config.model_name,
            messages=[{"role": "user", "content": prompt}],
            temperature=self.config.temperature,
            max_tokens=self.config.max_tokens,
        )

        return LLMResponse(
            content=response.choices[0].message.content,
            model=response.model,
            input_tokens=response.usage.prompt_tokens,
            output_tokens=response.usage.completion_tokens,
        )

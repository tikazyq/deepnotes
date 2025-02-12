import os
import time
from typing import Optional

from dotenv import load_dotenv
from openai import APIError, AzureOpenAI, OpenAI, RateLimitError
from openai.types.chat import ChatCompletion
from pydantic import BaseModel, Field

from deepnotes.config.config import get_config

load_dotenv()  # Load environment variables from .env


class LLMResponse(BaseModel):
    content: str
    provider: str
    model: str
    input_tokens: int
    output_tokens: int
    model_instance: Optional[BaseModel] = None


def get_provider_config(provider: str, model: str) -> dict:
    config = get_config()
    try:
        return config["llm"]["providers"][provider][model]
    except KeyError:
        raise ValueError(
            f"Configuration not found for provider '{provider}' and model '{model}'"
        )


class LLMConfig(BaseModel):
    provider: str
    model: str
    base_url: Optional[str]
    api_key: Optional[str]
    api_version: Optional[str]
    language: str = Field(default="english", description="Output language for responses")
    concurrency: Optional[dict] = None  # Add concurrency config


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
            return AzureOpenAI(**client_params)

        return OpenAI(**client_params)

    def generate(
        self,
        prompt: str,
        parse_json: bool = True,
        response_model: Optional[
            type[BaseModel]
        ] = None,
    ) -> LLMResponse:
        """Generate response with automatic validation and self-correction"""
        original_response = self._generate_with_retry(prompt, parse_json)

        if response_model:
            try:
                instance = response_model.model_validate_json(original_response.content)
                return LLMResponse(
                    content=original_response.content,
                    provider=original_response.provider,
                    model=original_response.model,
                    input_tokens=original_response.input_tokens,
                    output_tokens=original_response.output_tokens,
                    model_instance=instance,
                )
            except Exception as e:
                print(f"Validation error: {str(e)}. Attempting self-correction...")
                corrected_response = self._correct_and_validate(
                    prompt, original_response.content, e, response_model
                )
                corrected_response.input_tokens += original_response.input_tokens
                corrected_response.output_tokens += original_response.output_tokens
                return corrected_response

        return original_response

    def _generate_with_retry(self, prompt: str, parse_json: bool) -> LLMResponse | None:
        """Base generation method with retry logic"""
        max_retries = 5
        backoff_factor = 1.5
        for attempt in range(max_retries):
            try:
                # Modified system prompt with language adaptation
                if self.config.language.lower() == "chinese":
                    system_content = "你是一个全能助手，能够理解所有语言，但请始终使用中文回答。你的回答必须使用中文，并保持简洁准确。"
                else:
                    system_content = f"You are a helpful assistant who understands all languages. Please respond in {self.config.language} using clear and natural expressions."

                messages = [
                    {
                        "role": "system",
                        "content": system_content
                    },
                    {
                        "role": "user",
                        "content": prompt
                    }
                ]

                response: ChatCompletion = self.client.chat.completions.create(
                    model=self.config.model,
                    messages=messages,
                )

                # Handle streaming response
                response_content = response.choices[0].message.content
                input_tokens = response.usage.prompt_tokens
                output_tokens = response.usage.completion_tokens

                # Original non-streaming handling
                if "</think>" in response_content and "<think>" in response_content:
                    response_content = response_content.split("</think>")[1]

                if parse_json and response_content.strip().startswith("```"):
                    response_content = self._extract_json_from_markdown(
                        response_content.strip()
                    )

                return LLMResponse(
                    content=response_content,
                    provider=self.config.provider,
                    model=self.config.model,
                    input_tokens=input_tokens,
                    output_tokens=output_tokens,
                )
            except RateLimitError:
                if attempt >= max_retries - 1:
                    raise
                sleep_time = backoff_factor ** attempt
                print(f"Rate limited, retrying in {sleep_time:.1f}s...")
                time.sleep(sleep_time)
            except APIError as e:
                if e.status_code == 502 and attempt < max_retries - 1:
                    continue  # Retry on bad gateway
                raise

    def _correct_and_validate(
        self,
        original_prompt: str,
        invalid_json: str,
        error: Exception,
        response_model: type[BaseModel],
        max_retries: int = 3,
        previous_errors: list[str] = None,
    ) -> LLMResponse:
        """Handle JSON correction and validation with error history"""
        previous_errors = previous_errors or []
        previous_errors.append(f"Error: {str(error)}\nInvalid JSON: {invalid_json}")

        if max_retries <= 0:
            raise ValueError(
                "Max retries exceeded. Errors:\n" + "\n".join(previous_errors)
            )

        error_history = "\n".join(
            [f"Attempt {i + 1}: {e}" for i, e in enumerate(previous_errors)]
        )

        correction_prompt = f"""
        JSON validation failed multiple times. Error history:
        {error_history}

        Original prompt: {original_prompt}

        Please carefully correct the JSON to match the required schema.
        Respond ONLY with the corrected JSON between ```json markers.
        """

        corrected_response = self._generate_with_retry(
            correction_prompt, parse_json=True
        )

        try:
            instance = response_model.model_validate_json(corrected_response.content)
            return LLMResponse(
                content=corrected_response.content,
                provider=corrected_response.provider,
                model=corrected_response.model,
                input_tokens=corrected_response.input_tokens,
                output_tokens=corrected_response.output_tokens,
                model_instance=instance,
            )
        except Exception as e:
            print(f"Correction failed (attempts left: {max_retries - 1}), retrying...")
            return self._correct_and_validate(
                original_prompt,
                corrected_response.content,
                e,
                response_model,
                max_retries - 1,
                previous_errors,
            )

    @staticmethod
    def _extract_json_from_markdown(markdown_text: str) -> str:
        """
        使用第三方库解析markdown中的JSON内容
        """
        from marko import Markdown
        from marko.block import FencedCode

        md = Markdown().parse(markdown_text)
        for child in md.children:
            if isinstance(child, FencedCode) and child.lang == "json":
                return child.children[-1].children.strip()
        return markdown_text  # Fallback to raw text


def get_llm_model(provider: str = None, model: str = None) -> LLMWrapper:
    config = get_config()
    if not provider:
        provider = config["llm"].get("default_provider")
    if not model:
        model = config["llm"].get("default_model")

    if provider and model:
        provider_config = get_provider_config(provider, model)
        llm_config = LLMConfig(
            provider=provider,
            model=provider_config["model_name"],
            base_url=provider_config["base_url"],
            api_key=os.getenv(
                f"{provider.upper()}_API_KEY", provider_config.get("api_key")
            ),
            api_version=provider_config.get("api_version"),
        )
    else:
        raise ValueError("Provider and model must be specified or set as default.")

    return LLMWrapper(llm_config)

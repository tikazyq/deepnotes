import os
from pathlib import Path
from typing import Optional

import yaml
from dotenv import load_dotenv
from openai import AzureOpenAI, OpenAI
from openai.types.chat import ChatCompletion
from pydantic import BaseModel

load_dotenv()  # Load environment variables from .env


class LLMResponse(BaseModel):
    content: str
    model: str
    input_tokens: int
    output_tokens: int
    model_instance: Optional[BaseModel] = None


def load_config():
    config_path = Path("config.local.yml")
    if not config_path.exists():
        config_path = Path("config.example.yml")

    with open(config_path) as f:
        config = yaml.safe_load(f)
    return config


def get_provider_config(provider: str, model: str) -> dict:
    config = load_config()
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
        ] = None,  # Add response model parameter
    ) -> LLMResponse:
        """Generate response with automatic validation and self-correction"""
        original_response = self._generate_with_retry(prompt, parse_json)

        if response_model:
            try:
                instance = response_model.model_validate_json(original_response.content)
                return LLMResponse(
                    content=original_response.content,
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

    def _generate_with_retry(self, prompt: str, parse_json: bool) -> LLMResponse:
        """Base generation method with retry logic"""
        response: ChatCompletion = self.client.chat.completions.create(
            model=self.config.model,
            messages=[{"role": "user", "content": prompt}],
        )

        response_content = response.choices[0].message.content
        if "<think>" and "</think>" in response_content:
            response_content = response_content.split("</think>")[1]

        if parse_json and response_content.strip().startswith("```"):
            response_content = self._extract_json_from_markdown(
                response_content.strip()
            )

        return LLMResponse(
            content=response_content,
            model=response.model,
            input_tokens=response.usage.prompt_tokens,
            output_tokens=response.usage.completion_tokens,
        )

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
    config = load_config()
    if not provider:
        provider = config["llm"].get("default_provider")
    if not model:
        model = config["llm"].get("default_model")

    if provider and model:
        provider_config = get_provider_config(provider, model)
        llm_config = LLMConfig(
            provider=provider_config["provider"],
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

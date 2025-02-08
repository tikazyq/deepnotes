from autogen_ext.models.openai import OpenAIChatCompletionClient


def get_model_client(
    base_url: str = "https://api.groq.com/openai/v1",
    model_name: str = "deepseek-r1-distill-llama-70b",
    api_key: str = "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",
) -> OpenAIChatCompletionClient:
    return OpenAIChatCompletionClient(
        base_url=base_url,
        model=model_name,
        api_key=api_key,
        model_info={
            "vision": False,
            "function_calling": True,
            "json_output": True,
            "family": "llama",
        },
    )

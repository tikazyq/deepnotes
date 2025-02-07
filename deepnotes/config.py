"""
配置管理模块，支持 YAML 格式的配置文件解析
"""

from dataclasses import field
from pathlib import Path
from typing import List, Optional

import yaml
from pydantic import BaseModel

from deepnotes.data_processing import DataSourceConfig


class LLMConfig(BaseModel):
    provider: str
    model: str
    base_url: Optional[str] = None
    api_key: Optional[str] = None
    temperature: float = 0.7
    max_tokens: int = 1024


class AppConfig(BaseModel):
    database_uri: str
    max_iterations: int = 5
    output_formats: List[str] = field(default_factory=lambda: ["json", "markdown"])
    data_sources: List[DataSourceConfig]
    llm: LLMConfig


def load_config(config_path: str = "config.yml") -> AppConfig:
    """
    加载并解析配置文件
    """
    config_file = Path(config_path)
    if not config_file.exists():
        raise FileNotFoundError(f"Config file {config_path} not found")

    with open(config_file) as f:
        raw_config = yaml.safe_load(f)

    return AppConfig(**raw_config["deepnotes"])

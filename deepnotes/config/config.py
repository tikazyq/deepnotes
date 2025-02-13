import os
from pathlib import Path

import yaml


def load_config():
    """Load configuration with environment-aware resolution similar to Vite"""
    env = os.getenv("APP_ENV", "development")
    config_paths = [
        Path("config.local.yml"),  # Local overrides (highest priority)
        Path(f"config.{env}.local.yml"),  # Environment-specific local overrides
        Path(f"config.{env}.yml"),  # Environment-specific config
        Path("config.yml"),  # Default configuration (lowest priority)
    ]

    for path in config_paths:
        if path.exists():
            with open(path) as f:
                return yaml.safe_load(f)

    raise FileNotFoundError(
        "No configuration file found in hierarchy: "
        + ", ".join(str(p) for p in config_paths)
    )


_config = load_config()


def get_config() -> dict:
    return _config

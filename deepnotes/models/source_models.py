from enum import Enum
from typing import Any, Dict, Optional

from pydantic import BaseModel, Field


class SourceType(str, Enum):
    DOCUMENT = "document"
    DATABASE = "database"
    CODEBASE = "codebase"
    API = "api"


class SourceConfig(BaseModel):
    type: SourceType
    name: str
    description: Optional[str] = None
    connection: Dict[str, Any] = Field(default_factory=dict)
    options: Dict[str, Any] = Field(default_factory=dict)

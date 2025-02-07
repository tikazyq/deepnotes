from datetime import datetime
from typing import Any, Dict, List

from pydantic import BaseModel


class DataEntity(BaseModel):
    id: str
    type: str
    attributes: Dict[str, Any]
    source: str
    confidence: float = 1.0
    metadata: Dict[str, str]


class IntermediateDataModel(BaseModel):
    """Unified intermediate representation"""

    timestamp: datetime = datetime.now()
    entities: List[DataEntity] = []
    relationships: List[Dict[str, str]] = []
    metadata: Dict[str, Any] = {}
    notes: List[Dict[str, Any]] = []  # Track note versions
    iteration_history: List[Dict[str, Any]] = []
    validation_summary: Dict[str, Any] = {}

from datetime import datetime
from typing import Any, Dict, List

from pydantic import BaseModel, Field


class DataEntity(BaseModel):
    id: str
    type: str
    attributes: Dict[str, Any] = Field(default_factory=dict)
    source: str
    confidence: float = 1.0
    metadata: Dict[str, str] = Field(default_factory=dict)


class Relationship(BaseModel):
    """Represents a connection between two DataEntities"""

    source: str  # ID of source entity
    target: str  # ID of target entity
    type: str  # Relationship type (e.g., "belongs_to", "depends_on")
    attributes: Dict[str, Any] = Field(default_factory=dict)
    confidence: float = 1.0
    metadata: Dict[str, str] = Field(default_factory=dict)


class ProcessedDataModel(BaseModel):
    """Unified processed data model"""

    raw_data: List[str] = Field(default_factory=list)
    raw_texts: List[str] = Field(default_factory=list)
    metadata: Dict[str, Any] = Field(default_factory=dict)


class IntermediateDataModel(BaseModel):
    """Unified intermediate representation"""

    timestamp: datetime = datetime.now()
    entities: List[DataEntity] = Field(default_factory=list)
    relationships: List[Relationship] = Field(default_factory=list)
    raw_data: List[Dict] = Field(default_factory=list)
    notes: List[str] = Field(default_factory=list)
    iteration_history: List[Dict] = Field(default_factory=list)
    metadata: List[Dict] = Field(default_factory=list)

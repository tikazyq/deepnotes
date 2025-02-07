from datetime import datetime
from typing import Any, Dict, List, Union

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


class IntermediateDataModel(BaseModel):
    """Unified intermediate representation"""

    timestamp: datetime = datetime.now()
    entities: List[DataEntity] = Field(default_factory=list)
    relationships: List[Relationship] = Field(default_factory=list)
    raw_data: List[Dict] = Field(default_factory=list)
    notes: List[str] = Field(default_factory=list)
    iteration_history: List[Dict] = Field(default_factory=list)
    metadata: List[Dict] = Field(default_factory=list)
    processing_stats: Dict = Field(default_factory=dict)
    validation_summary: Dict[str, Any] = Field(default_factory=dict)

    class Config:
        arbitrary_types_allowed = True


class NoteData(BaseModel):
    content: Union[dict, str, list]  # Flexible content storage
    format_version: str = "1.0"
    source_type: str  # code/database/document
    relationships: List[dict] = []  # For knowledge graph storage
    metadata: dict = {}
    output_format: str = 'json'  # json/text/kg
    
    def to_downstream_format(self, target_format: str):
        """Convert notes to requested downstream format"""
        converter = FormatConverter(self.content)
        return converter.convert(target_format)

    def add_relationship(self, entity_a, entity_b, relation_type):
        """Build knowledge graph relationships"""
        self.relationships.append({
            'source': entity_a,
            'target': entity_b,
            'type': relation_type
        })

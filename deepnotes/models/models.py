from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


class Entity(BaseModel):
    id: str = Field(
        description="Unique identifier for the entity. Naming convention: snake_case of entity name"
    )
    name: str = Field(description="Name of the entity")
    description: Optional[str] = Field(
        default=None, description="Description of the entity, if available"
    )
    type: str = Field(
        description="Type of the entity (e.g., 'concept', 'company', etc.)"
    )
    attributes: Optional[Dict[str, Any]] = Field(
        default_factory=dict, description="Additional attributes"
    )
    metadata: Optional[Dict[str, Any]] = Field(
        default_factory=dict, description="Metadata"
    )


class Relationship(BaseModel):
    """Represents a connection between two DataEntities"""

    source: str = Field(description="ID of source entity")
    target: str = Field(description="ID of target entity")
    type: str = Field(
        description="Relationship type (e.g., 'belongs_to', 'depends_on')"
    )
    attributes: Optional[Dict[str, Any]] = Field(
        default_factory=dict, description="Additional attributes"
    )
    metadata: Optional[Dict[str, str]] = Field(
        default_factory=dict, description="Metadata"
    )

    @property
    def id(self):
        return f"{self.source}__{self.type}__{self.target}"


class KnowledgeGraph(BaseModel):
    """Knowledge graph representation"""

    entities: List[Entity] = Field(default_factory=list)
    relationships: List[Relationship] = Field(default_factory=list)


class LoadedData(BaseModel):
    """Loaded data from loader"""

    metadata: Dict[str, Any] = Field(default_factory=dict)
    raw_data: List[Dict[str, Any]] = Field(default_factory=list)


class ChunkAnalysisResult(BaseModel):
    """Analysis result for each chunk of the target item"""

    chunk_index: Optional[int] = Field(
        default=None, description="Index of the chunk in the document"
    )
    summary: Optional[str] = Field(
        default=None, description="Summary of the text chunk"
    )
    core_topics: List[str] = Field(
        default_factory=list, description="Core topics extracted from the chunk"
    )
    key_entities: List[str] = Field(
        default_factory=list, description="Key entities extracted from the chunk"
    )


class ConsolidationAnalysisResult(BaseModel):
    """Consolidation analysis result of the target item"""

    knowledge_graph: Optional[KnowledgeGraph] = Field(
        default=None, description="Knowledge graph of the target item"
    )
    summary: Optional[str] = Field(
        default=None,
        description="Overall document summary. Should be comprehensive and provide an overview of the target item.",
    )
    metadata: Dict[str, Any] = Field(
        default_factory=dict, description="Metadata information"
    )

from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field


class Entity(BaseModel):
    id: str
    name: str
    type: str
    attributes: Optional[Dict[str, Any]] = Field(
        default_factory=dict, description="Additional attributes"
    )
    metadata: Optional[Dict[str, str]] = Field(
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


class KnowledgeGraph(BaseModel):
    """Knowledge graph representation"""

    entities: List[Entity] = Field(default_factory=list)
    relationships: List[Relationship] = Field(default_factory=list)


class LoadedData(BaseModel):
    """Loaded data from loader"""

    metadata: Dict[str, Any] = Field(default_factory=dict)
    raw_data: List[Dict[str, Any]] = Field(default_factory=list)


class ChunkAnalysisResult(BaseModel):
    """Chunk analysis result"""

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
    """Consolidation analysis result of chunks"""

    knowledge_graph: Optional[KnowledgeGraph] = Field(default=None)
    summary: Optional[str] = Field(default=None)
    metadata: Dict[str, Any] = Field(default_factory=dict)

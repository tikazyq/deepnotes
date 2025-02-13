from typing import Any, Dict, List, Optional

from pydantic import BaseModel, Field

from deepnotes.models.source_models import SourceType


class FileMetadata(BaseModel):
    """Common metadata for all file types"""

    file_path: str
    file_size: int
    file_type: str
    created_at: Optional[str] = None
    updated_at: Optional[str] = None


class CodeFile(BaseModel):
    """Represents a code file with its content and analysis"""

    path: str
    content: str
    extension: str
    metadata: FileMetadata
    analysis: Optional[Dict[str, List[str]]] = Field(
        default_factory=dict,
        description="Analysis results containing classes, functions, and imports",
    )


class TableSchema(BaseModel):
    """Database table schema representation"""

    name: str
    schema: Dict[str, Any]
    sample_data: List[Dict[str, Any]]
    metadata: FileMetadata


class DocumentChunk(BaseModel):
    """Document chunk with its text content"""

    text: str
    index: int
    metadata: Optional[Dict[str, Any]] = Field(default_factory=dict)


class ProcessedDocument(BaseModel):
    """Processed document with its chunks"""

    metadata: FileMetadata
    chunks: List[DocumentChunk]
    raw_content: Optional[str] = None


class LoadedData(BaseModel):
    """Base class for all loaded data"""

    source_type: SourceType
    global_metadata: Dict[str, Any] = Field(
        default_factory=dict, description="Common metadata across all loaded content"
    )
    data_type: str = Field(
        default="generic", description="Type identifier for polymorphic processing"
    )


class DocumentLoadedData(LoadedData):
    """Structured document data with chunk hierarchy"""

    documents: List[ProcessedDocument] = Field(
        ..., description="List of processed documents with chunks and metadata"
    )
    data_type: str = Field(default="documents", frozen=True)


class CodebaseLoadedData(LoadedData):
    """Structured codebase analysis results"""

    files: List[Dict[str, Any]] = Field(
        ..., description="List of code files with analyses"
    )
    dependencies: List[str] = Field(
        default_factory=list, description="Discovered package dependencies"
    )
    data_type: str = Field(default="codebase", frozen=True)


class DatabaseLoadedData(LoadedData):
    """Structured database schema information"""

    tables: List[Dict[str, Any]] = Field(
        ..., description="List of tables with schema and samples"
    )
    data_type: str = Field(default="database", frozen=True)

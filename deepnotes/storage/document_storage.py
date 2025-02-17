from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional

from sqlalchemy import JSON, DateTime, Integer, String, Text, create_engine, select, ForeignKey
from sqlalchemy.orm import DeclarativeBase, Mapped, Session, mapped_column, relationship


class Base(DeclarativeBase):
    pass


class Document(Base):
    __tablename__ = "documents"

    id: Mapped[int] = mapped_column(primary_key=True)
    path: Mapped[str] = mapped_column(String(500))
    hash: Mapped[str] = mapped_column(String(64))
    meta_info: Mapped[Dict] = mapped_column(JSON)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)
    updated_at: Mapped[datetime] = mapped_column(
        DateTime, default=datetime.utcnow, onupdate=datetime.utcnow
    )

    chunks: Mapped[List["DocumentChunk"]] = relationship(back_populates="document")
    analysis_results: Mapped[List["DocumentAnalysis"]] = relationship(
        back_populates="document"
    )


class DocumentChunk(Base):
    __tablename__ = "document_chunks"

    id: Mapped[int] = mapped_column(primary_key=True)
    document_id: Mapped[int] = mapped_column(Integer, ForeignKey("documents.id"))
    index: Mapped[int] = mapped_column(Integer)
    content: Mapped[str] = mapped_column(Text)
    meta_info: Mapped[Dict] = mapped_column(JSON)

    document: Mapped[Document] = relationship(back_populates="chunks")
    analysis_results: Mapped[List["ChunkAnalysis"]] = relationship(
        back_populates="chunk", cascade="all, delete-orphan"
    )


class DocumentAnalysis(Base):
    __tablename__ = "document_analyses"

    id: Mapped[int] = mapped_column(primary_key=True)
    document_id: Mapped[int] = mapped_column(Integer, ForeignKey("documents.id"))
    analysis_data: Mapped[Dict] = mapped_column(JSON)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    document: Mapped[Document] = relationship(back_populates="analysis_results")


class ChunkAnalysis(Base):
    __tablename__ = "chunk_analyses"

    id: Mapped[int] = mapped_column(primary_key=True)
    chunk_id: Mapped[int] = mapped_column(Integer, ForeignKey("document_chunks.id"))
    analysis_data: Mapped[Dict] = mapped_column(JSON)
    created_at: Mapped[datetime] = mapped_column(DateTime, default=datetime.utcnow)

    chunk: Mapped[DocumentChunk] = relationship(back_populates="analysis_results")


class DocumentStore:
    def __init__(self, database_url: str):
        # Enable SQLite foreign key support
        engine_args = {}
        if database_url.startswith("sqlite"):
            engine_args["connect_args"] = {"check_same_thread": False}
            
        self.engine = create_engine(database_url, **engine_args)
        Base.metadata.create_all(self.engine)

    def store_document(self, path: str, content_hash: str, metadata: Dict) -> Document:
        with Session(self.engine) as session:
            document = Document(
                path=str(Path(path).absolute()), hash=content_hash, meta_info=metadata
            )
            session.add(document)
            session.commit()
            return document

    def store_chunks(self, document_id: int, chunks: List[Dict]) -> List[DocumentChunk]:
        with Session(self.engine) as session:
            chunk_objects = []
            for idx, chunk in enumerate(chunks):
                chunk_obj = DocumentChunk(
                    document_id=document_id,
                    index=idx,
                    content=chunk["text"],
                    meta_info=chunk.get("metadata", {}),
                )
                chunk_objects.append(chunk_obj)
            session.add_all(chunk_objects)
            session.commit()
            return chunk_objects

    def store_analysis(self, document_id: int, analysis_data: Dict) -> DocumentAnalysis:
        with Session(self.engine) as session:
            analysis = DocumentAnalysis(
                document_id=document_id, analysis_data=analysis_data
            )
            session.add(analysis)
            session.commit()
            return analysis

    def store_chunk_analysis(self, chunk_id: int, analysis_data: Dict) -> ChunkAnalysis:
        with Session(self.engine) as session:
            analysis = ChunkAnalysis(chunk_id=chunk_id, analysis_data=analysis_data)
            session.add(analysis)
            session.commit()
            return analysis

    def get_document(self, document_id: int) -> Optional[Document]:
        with Session(self.engine) as session:
            return session.get(Document, document_id)

    def get_document_by_path(self, path: str) -> Optional[Document]:
        with Session(self.engine) as session:
            stmt = select(Document).where(Document.path == str(Path(path).absolute()))
            return session.execute(stmt).scalar_one_or_none()

    def get_chunks(self, document_id: int) -> List[DocumentChunk]:
        with Session(self.engine) as session:
            stmt = select(DocumentChunk).where(DocumentChunk.document_id == document_id)
            return session.execute(stmt).scalars().all()

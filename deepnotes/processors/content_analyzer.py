import hashlib
from asyncio import as_completed
from concurrent.futures import ThreadPoolExecutor
from typing import Dict, List

from tqdm import tqdm

from deepnotes.llm.llm_wrapper import get_llm_model
from deepnotes.loaders.document_loader import DocumentLoader
from deepnotes.models.analyzer_models import (
    ChunkAnalysisResult,
    ConsolidationAnalysisResult,
    Entity,
    KnowledgeGraph,
    Relationship,
)
from deepnotes.models.loader_models import (
    CodeFile,
    DocumentLoadedData,
    LoadedData,
    ProcessedDocument,
)
from deepnotes.models.source_models import SourceConfig, SourceType
from deepnotes.storage.document_storage import DocumentStore


class ContentAnalyzer:
    def __init__(self, config: dict = None):
        """
        Initializes the first-layer document processor.
        """
        self.llm_model = get_llm_model()
        self.config = config or {}

        # Get chunking config with defaults
        chunk_config = self.config.get("text_processing", {})
        self.loader = DocumentLoader(
            SourceConfig(
                type=SourceType.DOCUMENT,
                name="document_loader",
                options={
                    "chunk_size": chunk_config.get("chunk_size", 1000),
                    "chunk_overlap": chunk_config.get("chunk_overlap", 100),
                },
            )
        )

        # Set default concurrency if not configured
        self.doc_concurrency = self.config.get(
            "document_processing", 8,
        )
        self.chunk_concurrency = self.config.get(
            "chunk_processing", 4,
        )

        self.document_store = DocumentStore(
            self.config.get("database_uri", "sqlite:///deepnotes.db")
        )

        self.document_cache = {}

    def process_loaded_data(
        self, data: LoadedData
    ) -> List[ConsolidationAnalysisResult]:
        """Route processing based on data type"""
        if data.data_type == "documents":
            doc_data = DocumentLoadedData(**data.model_dump())
            return self._analyze_documents(doc_data.documents)
        elif data.data_type == "codebase":
            return self._analyze_codebase(data.files, data.dependencies)
        elif data.data_type == "database":
            return self._analyze_database(data.tables)
        return []

    @staticmethod
    def _calculate_file_hash(file_path: str) -> str:
        """Calculate SHA-256 hash of file content"""
        sha256_hash = hashlib.sha256()
        with open(file_path, "rb") as f:
            for byte_block in iter(lambda: f.read(4096), b""):
                sha256_hash.update(byte_block)
        return sha256_hash.hexdigest()

    def _consolidate_results(
        self, chunk_analysis_results: List[ChunkAnalysisResult]
    ) -> ConsolidationAnalysisResult | None:
        """
        Process chunk-level intermediate data list for data consolidation and final output generation (comprehensive summary).
        Args:
            chunk_analysis_results (list): List of chunk-level intermediate data generated by first layer
        Returns:
            str: Final output (comprehensive document summary)
        """
        if not chunk_analysis_results:
            raise ValueError(
                "No chunk-level data provided for consolidation processing."
            )

        prompt = self._create_consolidation_prompt(chunk_analysis_results)

        # Initialize progress with chunk count
        llm_response = self.llm_model.generate(
            prompt,
            response_model=ConsolidationAnalysisResult,
        )
        result = llm_response.model_instance

        return result

    @staticmethod
    def _create_chunk_analysis_prompt(chunk_content, chunk_index):
        """
        Construct chunk analysis prompt (example)
        """
        prompt_template = f"""Analyze the content of the following document chunk (chunk {
            chunk_index
        }) and extract the following information in JSON format:
        - Chunk index
        - Concise summary (under 500 words)
        - Core topics
        - Key entities

        Document chunk content:
        {chunk_content}

        Output JSON Schema:
        {ChunkAnalysisResult.model_json_schema()}

        Output JSON Example:
        {
            ChunkAnalysisResult(
                chunk_index=0,
                summary="This is a summary.",
                core_topics=["topic1", "topic2"],
                key_entities=["entity1", "entity2"],
            ).model_dump_json()
        }
        """
        return prompt_template

    @staticmethod
    def _create_consolidation_prompt(chunk_analysis_results: List[ChunkAnalysisResult]):
        """
        Construct document summary consolidation prompt (example)
        """
        prompt_template = f"""
        Based on the summaries from multiple document chunks below, generate a comprehensive document analysis including:
        - Overall summary
        - Knowledge graph structure
        - Metadata information

        Please be reminded:
        1. Return results in JSON format.
        2. Entity ID should be meaningful and unique, following snake case format. E.g. "knowledge_graph", "deep_learning".

        Document chunk information (in order):
        {[data.model_dump_json() for data in chunk_analysis_results]}

        Output JSON Schema:
        {ConsolidationAnalysisResult.model_json_schema()}

        Output JSON Example:
        {
            ConsolidationAnalysisResult(
                summary="This is a comprehensive document summary.",
                knowledge_graph=KnowledgeGraph(
                    entities=[
                        Entity(
                            id="knowledge_graph",
                            name="Knowledge Graph",
                            type="concept",
                        ),
                        Entity(
                            id="deep_learning",
                            name="Deep Learning",
                            type="concept",
                        ),
                    ],
                    relationships=[
                        Relationship(
                            source="knowledge_graph",
                            target="deep-learning",
                            type="depends_on",
                        ),
                    ],
                ),
            ).model_dump_json()
        }
        """
        return prompt_template

    def _analyze_chunk(self, chunk_content: str, chunk_index: int) -> ChunkAnalysisResult:
        """
        Analyze a single document chunk.
        """
        prompt = self._create_chunk_analysis_prompt(chunk_content, chunk_index)
        llm_response = self.llm_model.generate(
            prompt, response_model=ChunkAnalysisResult
        )

        analysis_result = ChunkAnalysisResult(
            **llm_response.model_instance.model_dump()
        )
        if analysis_result.chunk_index is None:
            analysis_result.chunk_index = chunk_index

        return analysis_result

    def _analyze_codebase(
        self, files: List[CodeFile], dependencies: List[str]
    ) -> List[ConsolidationAnalysisResult]:
        """Analyze codebase structure and relationships"""
        raise NotImplementedError("Codebase analysis not implemented yet")

    def _analyze_database(
        self, tables: List[Dict]
    ) -> List[ConsolidationAnalysisResult]:
        """Analyze database schema and data patterns"""
        raise NotImplementedError("Database analysis not implemented yet")

    def _analyze_documents(self, documents: List[ProcessedDocument]) -> List[ConsolidationAnalysisResult]:
        """Analyze document files and their structure using threading"""
        def process_document(doc: ProcessedDocument):
            with ThreadPoolExecutor(max_workers=self.chunk_concurrency) as chunk_executor:
                chunk_futures = [chunk_executor.submit(self._analyze_chunk, chunk.text, chunk.index) for chunk in doc.chunks]
                chunk_results = []
                for cf in tqdm(chunk_futures):
                    chunk_result = cf.result()
                    chunk_results.append(chunk_result)
                return self._consolidate_results(chunk_results)

        results = []
        with ThreadPoolExecutor(max_workers=self.doc_concurrency) as executor:
            futures = [executor.submit(process_document, doc) for doc in documents]
            with tqdm(total=len(futures), desc="Analyzing documents", unit="docs") as pbar:
                for future in as_completed(futures):
                    result = future.result()
                    if result:
                        results.append(result)
                    pbar.update(1)

        return results

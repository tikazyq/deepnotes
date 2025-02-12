import concurrent
import os
from concurrent.futures import ThreadPoolExecutor
from typing import List, Optional

from tqdm import tqdm

from deepnotes.llm.llm_wrapper import get_llm_model
from deepnotes.loaders.document_loader import DocumentLoader, DocumentProcessorConfig
from deepnotes.models.models import (
    ChunkAnalysisResult,
    ConsolidationAnalysisResult,
    Entity,
    KnowledgeGraph,
    Relationship,
)


class DocumentProcessor:
    def __init__(self, config: dict = None):
        """
        Initializes the first-layer document processor.
        """
        self.llm_model = get_llm_model()
        self.config = config or {}

        # Get chunking config with defaults
        chunk_config = self.config.get("text_processing", {})
        self.loader = DocumentLoader(
            DocumentProcessorConfig(
                chunk_size=chunk_config.get("chunk_size", 1000),
                chunk_overlap=chunk_config.get("chunk_overlap", 100),
            )
        )

        # Set default concurrency if not configured
        self.doc_concurrency = self.config.get("document_processing", os.cpu_count() or 4)
        self.chunk_concurrency = self.config.get("chunk_processing", os.cpu_count() or 4)

    def process(self, target_path: str):
        """
        Process a single document file or directory based on the input path.
        Args:
            target_path (str): Path to the document file or directory
        """
        if os.path.isfile(target_path):
            result = self.process_document(target_path)
            if result:
                return [result]
            return []
        elif os.path.isdir(target_path):
            return self.process_directory(target_path)
        else:
            raise ValueError("Invalid target path provided.")

    def process_directory(
        self, directory_path, recursive: bool = True
    ) -> Optional[List[ConsolidationAnalysisResult]]:
        """
        Process a directory of long document files.
        Args:
            directory_path (str): Path to the directory containing document files
            recursive (bool): Whether to process subdirectories recursively
        Returns:
            list[dict]: List of document-level analysis results, each element corresponds to a document's analysis result
        """
        document_paths = []
        for root_path, _, files in os.walk(directory_path):
            for file in files:
                document_paths.append(os.path.join(root_path, file))

        if not document_paths:
            return None

        if recursive:
            for document_path in document_paths:
                if os.path.isdir(document_path):
                    document_paths.extend(
                        self.process_directory(document_path, recursive=True) or []
                    )

        if not document_paths:
            return None

        fusion_results = []

        # Process documents in parallel
        with ThreadPoolExecutor(max_workers=self.doc_concurrency) as executor:
            futures = [
                executor.submit(self.process_document, document_path)
                for document_path in document_paths
            ]

            # Add progress bar for document processing
            with tqdm(total=len(futures), desc="Processing documents") as pbar:
                for future in concurrent.futures.as_completed(futures):
                    result = future.result()
                    if result:
                        fusion_results.append(result)
                    pbar.update(1)

        return fusion_results

    def process_document(self, document_path) -> Optional[ConsolidationAnalysisResult]:
        """
        Process a single long document file.
        Args:
            document_path (str): Path to the document file
        Returns:
            list[dict]: List of chunk-level intermediate data, each element corresponds to a chunk's analysis result
        """
        loaded_data = self.loader.process(document_path)
        document_chunks = loaded_data.raw_data

        analysis_results = []

        # Process chunks in parallel
        with ThreadPoolExecutor(max_workers=self.chunk_concurrency) as executor:
            futures = [
                executor.submit(self._analyze_chunk, chunk_content, chunk_index)
                for chunk_index, chunk_content in enumerate(document_chunks)
            ]

            # Add progress bar for chunk processing
            with tqdm(
                total=len(futures),
                desc=f"Processing chunks in {os.path.basename(document_path)}",
            ) as pbar:
                for future in concurrent.futures.as_completed(futures):
                    analysis_result = future.result()
                    if analysis_result and analysis_result.summary:
                        analysis_results.append(analysis_result)
                    pbar.update(1)

        # Maintain original order
        analysis_results.sort(key=lambda x: x.chunk_index)

        # Process chunk-level data for consolidation
        consolidated_result = self.process_chunk_data_list(analysis_results)

        return consolidated_result

    def process_chunk_data_list(
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
        total_chunks = len(chunk_analysis_results)
        with tqdm(total=total_chunks,
                 desc=f"Consolidating {total_chunks} chunks",
                 bar_format="{l_bar}{bar}| {n_fmt}/{total_fmt} chunks") as pbar:

            llm_response = self.llm_model.generate(
                prompt,
                response_model=ConsolidationAnalysisResult,
            )

            pbar.update(total_chunks)

        result = llm_response.model_instance
        return result

    def _analyze_chunk(self, chunk_content, chunk_index):
        """
        Analyze a single document chunk.
        """
        prompt = self._create_chunk_analysis_prompt(chunk_content, chunk_index)
        llm_response = self.llm_model.generate(
            prompt, response_model=ChunkAnalysisResult
        )

        analysis_result = llm_response.model_instance
        if analysis_result.chunk_index is None:
            analysis_result.chunk_index = chunk_index

        return analysis_result

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

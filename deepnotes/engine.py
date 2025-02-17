from typing import Dict, List, Type

from deepnotes.loaders.base_loader import BaseLoader
from deepnotes.loaders.codebase_loader import CodebaseLoader
from deepnotes.loaders.database_loader import DatabaseLoader
from deepnotes.loaders.document_loader import DocumentLoader
from deepnotes.loaders.web_document_loader import WebDocumentLoader
from deepnotes.models.source_models import SourceConfig, SourceType
from deepnotes.processors.content_analyzer import ContentAnalyzer
from deepnotes.processors.knowledge_processor import KnowledgeProcessor


class DeepNotesEngine:
    def __init__(self):
        self.content_analyzer = ContentAnalyzer()
        self.knowledge_processor = KnowledgeProcessor()
        self._register_loaders()

    def _register_loaders(self):
        """Register available source loaders"""
        self.loaders: Dict[SourceType, Type[BaseLoader]] = {
            SourceType.DOCUMENT: DocumentLoader,
            SourceType.DATABASE: DatabaseLoader,
            SourceType.CODEBASE: CodebaseLoader,
            SourceType.WEB_DOCUMENT: WebDocumentLoader,
        }

    def process_source(self, config: SourceConfig):
        """Process a single source using appropriate loader"""
        loader_class = self.loaders.get(config.type)
        if not loader_class:
            raise ValueError(f"No loader found for source type: {config.type}")

        loader = loader_class(config)
        if not loader.validate_config():
            raise ValueError(f"Invalid configuration for source: {config.name}")

        loaded_data = loader.process()
        processed_results = self.content_analyzer.process_loaded_data(loaded_data)
        return self.knowledge_processor.merge_analysis(processed_results)

    def run(self, sources: List[SourceConfig]):
        """Process multiple sources and merge results"""
        for source_config in sources:
            self.process_source(source_config)
        return self.knowledge_processor.get_knowledge_graph()


if __name__ == "__main__":
    engine = DeepNotesEngine()
    configs = [
        # SourceConfig(
        #     type=SourceType.DOCUMENT,
        #     name="documentation",
        #     connection={"path": os.getenv("DEEPNOTES_TARGET_PATH")},
        # ),
        # SourceConfig(
        #     type=SourceType.DATABASE,
        #     name="user_db",
        #     connection={
        #         "connection_string": "postgresql://user:pass@localhost/db",
        #         "schema": "public",
        #     },
        # ),
        # SourceConfig(
        #     type=SourceType.CODEBASE,
        #     name="backend",
        #     connection={"path": "src/"},
        #     options={"ignore_patterns": ["*.pyc", "__pycache__"]},
        # ),
        # SourceConfig(
        #     type=SourceType.WEB_DOCUMENT,
        #     name="docs",
        #     connection={
        #         'start_urls': ['https://docs.crawlab.cn/en/guide/'],
        #         'url_pattern': 'https://docs.crawlab.cn/en/guide/',
        #     },
        # ),
        SourceConfig(
            type=SourceType.WEB_DOCUMENT,
            name="docs",
            connection={
                'start_urls': ['https://microsoft.github.io/autogen/stable/user-guide/extensions-user-guide/index.html'],
                'url_pattern': 'https://microsoft.github.io/autogen/stable/user-guide/extensions-user-guide/',
            },
        ),
    ]
    knowledge_base = engine.run(configs)
    print(knowledge_base.model_dump_json(indent=2))

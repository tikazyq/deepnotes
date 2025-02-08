"""
数据预处理模块，包含不同数据类型的处理器
"""

from .base_processor import BaseDataProcessor, DataSourceConfig
from .database_processor import DatabaseProcessor, DatabaseProcessorConfig


class DataProcessorFactory:
    @staticmethod
    def create_processor(config: DataSourceConfig) -> BaseDataProcessor:
        """
        Factory method to create appropriate data processor based on config
        """
        processor_map = {
            "database": (DatabaseProcessor, DatabaseProcessorConfig),
            "document": (DocumentProcessor, DocumentProcessorConfig),
            # Add new processor types here
        }

        if config.source_type not in processor_map:
            raise ValueError(f"Unsupported data source type: {config.source_type}")

        processor_class, config_class = processor_map[config.source_type]

        # Validate config against the processor's specific config class
        validated_config = config_class(**config.dict())

        return processor_class(validated_config)

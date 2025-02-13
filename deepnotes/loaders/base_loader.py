"""
数据处理器基类模块
"""

from abc import ABC, abstractmethod

from deepnotes.models.loader_models import LoadedData
from deepnotes.models.source_models import SourceConfig, SourceType


class BaseLoader(ABC):
    def __init__(self, config: SourceConfig):
        self.config = config

    @abstractmethod
    def validate_config(self) -> bool:
        """Validate source configuration"""
        pass

    @abstractmethod
    def process(self, *args, **kwargs) -> LoadedData:
        """Process data and return structured results"""
        pass

    @classmethod
    @abstractmethod
    def get_source_type(cls) -> SourceType:
        """Return the type of source this loader handles"""
        pass

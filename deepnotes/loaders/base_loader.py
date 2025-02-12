"""
数据处理器基类模块
"""

from abc import ABC, abstractmethod

from deepnotes.models.models import LoadedData


class BaseLoader(ABC):
    @abstractmethod
    def process(self, *args, **kwargs) -> LoadedData:
        """Process data and return structured results"""
        pass

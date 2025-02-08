"""
数据处理器基类模块
"""

from abc import ABC, abstractmethod
from typing import Any, Dict

from pydantic import BaseModel


class DataSourceConfig(BaseModel):
    """Base configuration model for data sources"""

    source_type: str
    connection_params: Dict[str, Any]

    class Config:
        extra = "allow"  # Allow extra fields for processor-specific configs


class BaseDataProcessor(ABC):
    def __init__(self, config: DataSourceConfig):
        self.config = config
        self.raw_data = None
        self.processed_data = None

    @abstractmethod
    def extract(self):
        """Extract raw data from source"""
        pass

    def transform(self):
        """Default transformation logic"""
        # Common transformation steps here
        self.processed_data = self.raw_data  # Default to identity transform

    def load(self):
        """Load processed data to next stage"""
        return self.processed_data

    @abstractmethod
    def process(self):
        """Process data and return structured results"""
        pass

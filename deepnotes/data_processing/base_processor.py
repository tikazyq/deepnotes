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
    def process(self) -> Dict[str, Any]:
        """Process data and return structured results"""
        pass


class BaseProcessor:
    """Base class for all data processors"""
    def __init__(self, source_data):
        self.source_data = source_data
        self._validate_data()

    def _validate_data(self):
        """Ensure input data meets processor requirements"""
        if not self.source_data:
            raise ValueError("Empty source data provided")

    def preprocess(self):
        """Common preprocessing steps"""
        # Add language detection, formatting, etc. here
        raise NotImplementedError

    def generate_notes(self, **kwargs):
        """Generate initial structured notes"""
        # To be implemented by specific processors
        raise NotImplementedError

    def analyze_coverage(self, generated_notes):
        """Analyze information coverage vs source data"""
        # Common analysis framework
        raise NotImplementedError

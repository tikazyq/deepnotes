"""
数据库处理模块，用于解析数据库结构
"""

from typing import Any, Dict

import pandas as pd
import sqlalchemy
from sqlalchemy import create_engine, inspect, text

from .base_processor import BaseDataProcessor, DataSourceConfig


class DatabaseProcessorConfig(DataSourceConfig):
    query: str
    chunk_size: int = 1000

    class Config:
        extra = "forbid"  # Strict validation for database config


class DatabaseProcessor(BaseDataProcessor):
    def __init__(self, config: DatabaseProcessorConfig):
        super().__init__(config)
        self.engine = create_engine(config.connection_params["uri"])
        self.raw_data = None  # Add raw data storage
        self.schema_analyzer = SchemaAnalyzer()

    def extract(self):
        """Extract raw data from database"""
        with self.engine.connect() as conn:
            results = []
            for chunk in pd.read_sql(
                text(self.config.query), conn, chunksize=self.config.chunk_size
            ):
                results.append(chunk)
            self.raw_data = pd.concat(results)

    def process(self) -> Dict[str, Any]:
        """Full processing pipeline"""
        # First collect schema metadata
        schema = self._collect_schema_data()
        
        # Then extract raw data
        self.extract()
        
        # Process both schema and data
        return {
            "metadata": self._process_metadata(schema),
            "sample_data": self._process_raw_data()
        }

    def _collect_schema_data(self) -> Dict[str, Any]:
        """Collect database schema information using existing engine"""
        inspector = inspect(self.engine)
        schema = {"tables": {}, "relationships": [], "views": {}}

        # 获取表结构 (Get table structure)
        for table_name in inspector.get_table_names():
            schema["tables"][table_name] = {
                "columns": {
                    col["name"]: {
                        "type": str(col["type"]),
                        "nullable": col["nullable"]
                    }
                    for col in inspector.get_columns(table_name)
                },
                "primary_key": inspector.get_pk_constraint(table_name),
                "indexes": inspector.get_indexes(table_name),
            }

        # 获取外键关系 (Get foreign key relationships)
        for table_name in schema["tables"]:
            for fk in inspector.get_foreign_keys(table_name):
                schema["relationships"].append({
                    "source_table": table_name,
                    "target_table": fk["referred_table"],
                    "columns": fk["constrained_columns"],
                    "ref_columns": fk["referred_columns"],
                })

        return schema

    def _process_metadata(self, raw_schema: Dict[str, Any]) -> Dict[str, Any]:
        """Process schema metadata into standard format"""
        processed = {
            "entities": [],
            "relationships": raw_schema["relationships"]
        }

        for table_name, table_info in raw_schema["tables"].items():
            entity = {
                "name": table_name,
                "type": "table",
                "columns": table_info["columns"],
                "primary_key": table_info["primary_key"],
                "description": f"Database table {table_name} with {len(table_info['columns'])} columns",
            }
            processed["entities"].append(entity)

        return processed

    def _process_raw_data(self) -> Dict[str, Any]:
        """Process raw query results into analysis-friendly format"""
        if self.raw_data is None or self.raw_data.empty:
            return {}
            
        return {
            "stats": {
                "row_count": len(self.raw_data),
                "column_stats": self._calculate_column_stats(),
            },
            "sample_records": self.raw_data.head(10).to_dict(orient="records")
        }

    def _calculate_column_stats(self) -> Dict[str, Any]:
        """Calculate basic statistics for numeric columns"""
        stats = {}
        for col in self.raw_data.select_dtypes(include='number').columns:
            stats[col] = {
                "min": float(self.raw_data[col].min()),
                "max": float(self.raw_data[col].max()),
                "mean": float(self.raw_data[col].mean()),
                "null_count": int(self.raw_data[col].isnull().sum())
            }
        return stats

    def preprocess(self):
        """Database-specific preprocessing"""
        # Remove system tables, normalize casing, etc.
        self.clean_data = self._remove_system_tables()
        return self.clean_data

    def generate_notes(self, detail_level='basic'):
        """Generate structured database notes"""
        notes = {
            'tables': [],
            'relationships': [],
            'constraints': []
        }

        for table in self.clean_data:
            table_note = {
                'name': table.name,
                'columns': self._process_columns(table.columns),
                'indexes': [idx.to_dict() for idx in table.indexes]
            }
            if detail_level == 'advanced':
                table_note['usage_patterns'] = self._analyze_usage_patterns(table)
            notes['tables'].append(table_note)
        
        notes['relationships'] = self.schema_analyzer.detect_relationships(self.clean_data)
        return notes

    def _analyze_usage_patterns(self, table):
        """Advanced analysis of table usage patterns"""
        # Implementation for query pattern analysis

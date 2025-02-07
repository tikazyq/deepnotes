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

    def extract(self):
        with self.engine.connect() as conn:
            results = []
            for chunk in pd.read_sql(
                text(self.config.query), conn, chunksize=self.config.chunk_size
            ):
                results.append(chunk)
            self.raw_data = pd.concat(results)

    def collect_data(self, source: str) -> Dict[str, Any]:
        """连接数据库并获取元数据"""
        engine = sqlalchemy.create_engine(f"{source}")
        inspector = inspect(engine)

        schema = {"tables": {}, "relationships": [], "views": {}}

        # 获取表结构
        for table_name in inspector.get_table_names():
            schema["tables"][table_name] = {
                "columns": [
                    {
                        col["name"]: str(col["type"])
                        for col in inspector.get_columns(table_name)
                    }
                ],
                "primary_key": inspector.get_pk_constraint(table_name),
                "indexes": inspector.get_indexes(table_name),
            }

        # 获取外键关系
        for table_name in schema["tables"]:
            for fk in inspector.get_foreign_keys(table_name):
                schema["relationships"].append(
                    {
                        "source_table": table_name,
                        "target_table": fk["referred_table"],
                        "columns": fk["constrained_columns"],
                        "ref_columns": fk["referred_columns"],
                    }
                )

        return schema

    def process(self, raw_data: Dict[str, Any]) -> Dict[str, Any]:
        """对数据库元数据进行预处理"""
        processed = {"entities": [], "relationships": raw_data["relationships"]}

        for table_name, table_info in raw_data["tables"].items():
            entity = {
                "name": table_name,
                "type": "table",
                "columns": table_info["columns"],
                "primary_key": table_info["primary_key"],
                "description": f"Database table {table_name} with {len(table_info['columns'])} columns",
            }
            processed["entities"].append(entity)

        return processed

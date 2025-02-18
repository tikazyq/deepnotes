from typing import Dict, List

import sqlalchemy as sa
from sqlalchemy import MetaData, inspect

from deepnotes.loaders.base_loader import BaseLoader
from deepnotes.models.loader_models import DatabaseLoadedData, FileMetadata, TableSchema
from deepnotes.models.source_models import (
    SourceConfig,
    SourceType,
)


class DatabaseLoader(BaseLoader):
    def __init__(self, config: SourceConfig):
        super().__init__(config)
        self.engine = None
        self.metadata = MetaData()

    @classmethod
    def get_source_type(cls) -> SourceType:
        return SourceType.DATABASE

    def validate_config(self) -> bool:
        required = {"connection_string", "schema"}
        return all(k in self.config.connection for k in required)

    def _connect(self):
        self.engine = sa.create_engine(self.config.connection["connection_string"])

    def _extract_schema(self) -> Dict:
        inspector = inspect(self.engine)
        schema = self.config.connection.get("schema")

        tables = {}
        for table_name in inspector.get_table_names(schema=schema):
            tables[table_name] = {
                "columns": inspector.get_columns(table_name, schema=schema),
                "primary_keys": inspector.get_pk_constraint(table_name, schema=schema),
                "foreign_keys": inspector.get_foreign_keys(table_name, schema=schema),
                "indexes": inspector.get_indexes(table_name, schema=schema),
            }
        return tables

    def _extract_sample_data(self, table_name: str, limit: int = 5) -> List[Dict]:
        table = sa.Table(
            table_name, self.metadata, schema=self.config.connection.get("schema")
        )
        query = sa.select(table).limit(limit)
        with self.engine.connect() as conn:
            result = conn.execute(query)
            return [dict(row) for row in result]

    def process(self) -> DatabaseLoadedData:
        self._connect()
        schema_info = self._extract_schema()

        processed_tables = []
        for table_name, table_info in schema_info.items():
            processed_tables.append(
                TableSchema(
                    name=table_name,
                    schema_data=table_info,
                    sample_data=self._extract_sample_data(table_name),
                    metadata=FileMetadata(
                        file_path=f"{self.config.name}.{table_name}",
                        file_size=0,  # Not applicable for DB tables
                        file_type="database_table",
                    ),
                )
            )

        return DatabaseLoadedData(
            source_type=SourceType.DATABASE,
            tables=processed_tables,
            global_metadata={
                "source": self.config.name,
                "type": "database",
                "total_tables": len(processed_tables),
            },
        )

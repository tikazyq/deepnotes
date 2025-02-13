import ast
import os
from typing import Dict, Set

from deepnotes.loaders.base_loader import BaseLoader
from deepnotes.models.loader_models import CodebaseLoadedData, CodeFile, FileMetadata
from deepnotes.models.source_models import SourceType


class CodebaseLoader(BaseLoader):
    @classmethod
    def get_source_type(cls) -> SourceType:
        return SourceType.CODEBASE

    def validate_config(self) -> bool:
        return "path" in self.config.connection

    def _get_ignore_patterns(self) -> Set[str]:
        return set(
            self.config.options.get(
                "ignore_patterns",
                ["__pycache__", "*.pyc", ".git", ".env", "venv", ".idea"],
            )
        )

    def _should_ignore(self, path: str, ignore_patterns: Set[str]) -> bool:
        for pattern in ignore_patterns:
            if pattern in path:
                return True
        return False

    def _extract_python_info(self, content: str) -> Dict:
        try:
            tree = ast.parse(content)
            return {
                "classes": [
                    node.name
                    for node in ast.walk(tree)
                    if isinstance(node, ast.ClassDef)
                ],
                "functions": [
                    node.name
                    for node in ast.walk(tree)
                    if isinstance(node, ast.FunctionDef)
                ],
                "imports": [
                    node.names[0].name
                    for node in ast.walk(tree)
                    if isinstance(node, ast.Import)
                ],
            }
        except:
            return {}

    def _process_file(self, file_path: str) -> CodeFile:
        """Process individual file with metadata"""
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()

        metadata = FileMetadata(
            file_path=file_path,
            file_size=os.path.getsize(file_path),
            file_type=os.path.splitext(file_path)[1],
            created_at=str(os.path.getctime(file_path)),
            updated_at=str(os.path.getmtime(file_path)),
        )

        return CodeFile(
            path=file_path,
            content=content,
            extension=os.path.splitext(file_path)[1],
            metadata=metadata,
            analysis=self._extract_python_info(content)
            if file_path.endswith(".py")
            else None,
        )

    def process(self) -> CodebaseLoadedData:
        base_path = self.config.connection["path"]
        ignore_patterns = self._get_ignore_patterns()

        if not os.path.exists(base_path):
            raise FileNotFoundError(f"Path not found: {base_path}")

        processed_files = []
        dependencies = set()

        for root, _, files in os.walk(base_path):
            for file in files:
                file_path = os.path.join(root, file)
                if not self._should_ignore(file_path, ignore_patterns):
                    try:
                        processed_file = self._process_file(file_path)
                        processed_files.append(processed_file)
                        if processed_file.analysis:
                            dependencies.update(
                                processed_file.analysis.get("imports", [])
                            )
                    except Exception as e:
                        print(f"Error processing {file_path}: {e}")

        return CodebaseLoadedData(
            source_type=SourceType.CODEBASE,
            files=processed_files,
            dependencies=list(dependencies),
            global_metadata={
                "codebase_root": self.config.connection["path"],
                "total_files": len(processed_files),
            },
        )

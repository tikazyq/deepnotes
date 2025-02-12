import os

from pydantic import BaseModel, Field

from deepnotes.loaders.base_loader import BaseLoader
from deepnotes.models.models import LoadedData

# PDF processing: PyPDF2
try:
    import PyPDF2
    from PyPDF2 import PdfReader
except ImportError:
    PyPDF2 = None
    PdfReader = None

# Word processing: python-docx
try:
    from docx import Document
except ImportError:
    Document = None

# PPTX processing: python-pptx
try:
    from pptx import Presentation
except ImportError:
    Presentation = None

# Excel processing: pandas
try:
    import pandas as pd
except ImportError:
    pd = None

# Text splitting from llama-index
try:
    from llama_index.core.node_parser import TokenTextSplitter
except ImportError:
    TokenTextSplitter = None


class DocumentProcessorConfig(BaseModel):
    chunk_size: int = Field(default=1000)
    chunk_overlap: int = Field(default=100)


class DocumentLoader(BaseLoader):
    def __init__(self, config: DocumentProcessorConfig = None):
        self.config = config or DocumentProcessorConfig()

    @staticmethod
    def _extract_pdf(file_path: str) -> str:
        """Extract text from PDF using PyPDF2"""
        text = []
        with open(file_path, "rb") as f:
            reader = PdfReader(f)
            for page in reader.pages:
                page_text = page.extract_text()
                if page_text:
                    text.append(page_text)
        return "\n".join(text)

    @staticmethod
    def _extract_docx(file_path: str) -> str:
        """Extract text from Word documents"""
        doc = Document(file_path)
        return "\n".join([para.text for para in doc.paragraphs])

    @staticmethod
    def _extract_pptx(file_path: str) -> str:
        """Extract text from PowerPoint presentations"""
        prs = Presentation(file_path)
        text = []
        for slide in prs.slides:
            for shape in slide.shapes:
                if hasattr(shape, "text"):
                    text.append(shape.text)
        return "\n".join(text)

    @staticmethod
    def _extract_excel(file_path: str) -> str:
        """Extract text from Excel files using pandas DataFrames"""
        xls = pd.ExcelFile(file_path)
        text = []
        for sheet_name in xls.sheet_names:
            df = xls.parse(sheet_name)
            text.append(f"=== Sheet: {sheet_name} ===\n{df.to_string(index=False)}")
        return "\n\n".join(text)

    def process(self, file_path: str) -> LoadedData:
        """Extract raw text from document based on file type"""
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"Document not found: {file_path}")

        ext = os.path.splitext(file_path)[1].lower()
        if ext == ".pdf":
            if not PyPDF2:
                raise ImportError("PyPDF2 required for PDF processing")
            content = self._extract_pdf(file_path)
        elif ext == ".docx":
            if not Document:
                raise ImportError("python-docx required for Word processing")
            content = self._extract_docx(file_path)
        elif ext == ".pptx":
            if not Presentation:
                raise ImportError("python-pptx required for PPTX processing")
            content = self._extract_pptx(file_path)
        elif ext in (".xlsx", ".xls"):
            if not pd:
                raise ImportError("pandas required for Excel processing")
            content = self._extract_excel(file_path)
        else:
            raise ValueError(f"Unsupported file type: {ext}")

        if not TokenTextSplitter:
            raise ImportError("llama-index required for text splitting")
        splitter = TokenTextSplitter(
            chunk_size=self.config.chunk_size, chunk_overlap=self.config.chunk_overlap
        )
        chunks = splitter.split_text(content)

        return LoadedData(
            metadata={"file_path": file_path},
            raw_data=[{"text": chunk} for index, chunk in enumerate(chunks)],
        )

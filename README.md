# DeepNotes

DeepNotes is an AI-powered knowledge extraction and organization framework that processes documents and builds a semantic knowledge graph using large language models (LLMs).

## Features

- **Document Processing**
  - Support for PDF, Word (DOCX), PowerPoint (PPTX), and Excel (XLSX) documents
  - Intelligent chunking with configurable size and overlap
  - Parallel processing of documents and chunks
  
- **Knowledge Extraction**
  - Entity and relationship extraction
  - Automated knowledge graph construction
  - Intelligent conflict resolution
  - Duplicate detection and merging

- **Storage Options**
  - In-memory storage for testing
  - NetworkX-based file storage
  - Neo4j graph database support

- **LLM Integration**
  - OpenAI API support
  - Azure OpenAI support
  - Configurable models and parameters
  - Multi-language support

## Installation

```bash
# Clone the repository
git clone https://github.com/tikazyq/deepnotes.git
cd deepnotes

# Create and activate virtual environment
python -m venv .venv
source .venv/bin/activate  # On Windows: .venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt

# For development
pip install -r requirements-dev.txt
```

## Configuration

1. Copy example configuration:
```bash
cp config.example.yml config.yml
```

2. Copy environment variables:
```bash
cp .env.example .env
```

3. Configure your API keys in `.env`:
```bash
# OpenAI API key
OPENAI_API_KEY=your_openai_api_key_here

# Azure OpenAI keys (if using Azure)
AZURE_API_KEY=your_azure_api_key_here
```

4. Configure your settings in `config.yml`:

```yaml
llm:
  default_provider: "openai"  # or "azure"
  default_model: "gpt-4"
  language: "english"  # or "chinese" for Chinese output
  providers:
    openai:
      gpt-4:
        api_key: "${OPENAI_API_KEY}"
    azure:
      gpt-4:
        base_url: "https://your-endpoint.openai.azure.com"
        api_key: "${AZURE_API_KEY}"
        api_version: "2024-02-15"

graph:
  storage_type: "memory"  # or "networkx", "neo4j"
  networkx:
    graph_file: "./data/knowledge_graph.graphml"
  neo4j:
    uri: "bolt://localhost:7687"
    user: "${NEO4J_USER}"
    password: "${NEO4J_PASSWORD}"
```

## Usage

Basic usage:

```python
from deepnotes import DeepNotesEngine

# Initialize engine
engine = DeepNotesEngine()

# Process a document or directory
knowledge_graph = engine.run("path/to/document.pdf")

# The result includes entities and relationships
print(knowledge_graph.model_dump_json(indent=2))
```

## Development

1. Install development dependencies:
```bash
pip install -r requirements-dev.txt
```

2. Run tests:
```bash
pytest
```

3. Format code:
```bash
ruff format .
```

## License

BSD 3-Clause License. See [LICENSE](LICENSE) for details.

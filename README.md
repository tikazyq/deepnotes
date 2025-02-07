# DeepNotes

DeepNotes is an AI-powered note generation system that combines multiple data sources with large language models (LLMs)
to create and iteratively refine structured notes through an intelligent processing pipeline.

## Key Features

- **Iterative Processing**: Multi-stage refinement of notes with convergence detection
- **Multi-source Data Integration**:
    - Database connectors (SQL)
    - Document processors (Markdown, PDF)
    - Code analysis
- **Structured Output**:
    - Technical reports
    - Executive summaries
    - Process audit trails
- **LLM Integration**:
    - OpenAI-compatible API support
    - Cost tracking and token monitoring
    - Customizable templates
- **Extensible Architecture**:
    - Plugin-based data processors
    - Configurable processing pipelines
    - Modular report generators

## Installation

```bash
git clone https://github.com/yourusername/deepnotes.git
cd deepnotes
pip install -r requirements.txt
```

## Configuration

Create `config.yml`:

```yaml
deepnotes:
  database_uri: "sqlite:///test.db"
  max_iterations: 3
  output_formats: [ "json", "markdown" ]
  data_sources:
    - source_type: "database"
      query: "SELECT * FROM employees"
      connection_params:
        uri: "sqlite:///test.db"
```

```yaml
deepnotes:
  database_uri: "sqlite:///notes.db"
  max_iterations: 5
  output_formats: [ "json", "markdown" ]
  data_sources:
    - source_type: "database"
      query: "SELECT FROM research_data"
      connection_params:
        uri: "postgresql://user:pass@localhost/research"
llm:
  provider: "openai" # or "groq", "azure", "ollama"
  model: "gpt-4-turbo"
  base_url: "https://api.example.com/v1" # For custom endpoints
  api_key: "${LLM_API_KEY}" # From environment
  temperature: 0.7
  max_tokens: 2000
```

## Usage

```python
from deepnotes import DeepNotesEngine

# Initialize processing engine
engine = DeepNotesEngine("config.yml")
engine.initialize_processors()

# Run full processing pipeline
report = engine.run_pipeline()

# Access outputs
print(report['executive_summary'])
print(f"Process iterations: {len(engine.data_model.iteration_history)}")
```

## Project Structure

```
deepnotes/
├── data_processing/ # Data connectors and processors
├── llm/ # LLM integration and wrappers
├── note_generation/ # Note templating and generation
├── iterative/ # Iterative optimization logic
├── reporting/ # Output formatting and templates
├── models.py # Core data models
├── config.py # Configuration management
└── main.py # Main processing engine
```

## Development

1. Set up virtual environment:

  ```bash
  python -m venv .venv
  source .venv/bin/activate
  ```

2. Install dependencies:

  ```bash
  pip install -r requirements.txt
  ```

3. Run tests:

  ```bash
  pytest
  ```

## Contributing

We welcome contributions! Please see our:

- [Contribution Guidelines](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Roadmap](ROADMAP.md)

## License

BSD 3-Clause License. See [LICENSE](LICENSE) for full text.

---

**Project Status**: Active Development (v0.0.1)

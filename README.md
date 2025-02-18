# DeepNotes

## Project Migration
This project has been migrated from Python to C#/.NET. The original Python codebase can be found in the `archive/python` directory.

## Project Structure
- `DeepNotes.Core` - Core business logic and domain entities
- `DeepNotes.DataLoaders` - Document loading from various sources
- `DeepNotes.KnowledgeGraph` - Knowledge graph processing and storage
- `DeepNotes.LLM` - LLM integration using Semantic Kernel
- `DeepNotes.Database` - Database abstraction and repositories
- `DeepNotes.API` - ASP.NET Core Web API backend

## Development Setup
1. Prerequisites:
   - .NET 8.0 SDK or later
   - Visual Studio 2022 or VS Code with C# extensions
   - Node.js (for React frontend)

2. Building the Solution:
   ```bash
   dotnet restore
   dotnet build
   ```

3. Running the API:
   ```bash
   cd DeepNotes.API
   dotnet run
   ```

## Original Python Project
The original Python implementation can be found in `archive/python/`. This code is maintained for reference but is no longer actively developed.

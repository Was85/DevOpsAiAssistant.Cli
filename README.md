# DevOps AI Assistant CLI

> **Work in Progress** - This project is under active development. Features and APIs may change.

A command-line tool that uses AI to analyze CI/CD pipeline YAML files and provide actionable feedback on security, performance, and best practices.

## What it does

The DevOps AI Assistant analyzes your Azure DevOps or GitHub Actions pipeline files and:

- Detects **security issues** (hardcoded secrets, missing security scans)
- Identifies **performance problems** (missing caching, expensive VM pools)
- Suggests **best practices** (testing, artifact publishing, code coverage)
- Provides **actionable suggestions** with specific fixes
- Optionally generates **improved YAML** based on recommendations

## How it works

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  Pipeline YAML  │────▶│  DevOps AI CLI   │────▶│   AI Provider   │
│  (your file)    │     │  (this tool)     │     │  (LLM analysis) │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                                │
                                ▼
                        ┌──────────────────┐
                        │  Analysis Report │
                        │  - Issues found  │
                        │  - Suggestions   │
                        │  - Fixed YAML    │
                        └──────────────────┘
```

1. You provide a pipeline YAML file (or use the demo)
2. The CLI sends it to an AI provider for analysis
3. The AI returns structured feedback as JSON
4. The CLI displays a formatted report with issues and suggestions

## Supported AI Providers

| Provider | Description | Configuration |
|----------|-------------|---------------|
| **OpenAI** | OpenAI API (default) | `OPENAI_API_KEY` |
| **Azure OpenAI** | Azure-hosted OpenAI | `OPENAI_API_KEY` + endpoint |
| **GitHub Models** | GitHub's AI inference | `GITHUB_TOKEN` |
| **Ollama** | Local LLM (no API key) | Endpoint only |

## Installation

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- An API key for your chosen AI provider

### Build from source

```bash
git clone https://github.com/Was85/DevOpsAiAssistant.Cli.git
cd DevOpsAiAssistant.Cli
dotnet build
```

## Usage

### Quick start (demo mode)

```bash
# Set your API key
export GITHUB_TOKEN=your-github-token

# Run with demo pipeline
dotnet run --project src/DevOpsAiAssistant.Cli -- analyze --demo
```

### Analyze a real pipeline

```bash
# Azure DevOps pipeline
dotnet run --project src/DevOpsAiAssistant.Cli -- analyze --file azure-pipelines.yml

# GitHub Actions workflow
dotnet run --project src/DevOpsAiAssistant.Cli -- analyze --file .github/workflows/build.yml --platform GitHubActions
```

### Command options

```
OPTIONS:
    -f, --file <FILE>        Path to the pipeline YAML file
    -p, --platform           Pipeline platform: AzureDevOps or GitHubActions
        --format             Output format: table or json
        --show-yaml          Display the suggested improved YAML
        --output <FILE>      Save suggested YAML to a file
        --demo               Run with a built-in sample pipeline
```

### Output example

```
╭─Pipeline Analysis─────────────────────────╮
│ azure-pipelines.yml | Platform: AzureDevOps │
╰───────────────────────────────────────────╯

╭─Summary──────────────────────────────────────────────────────────╮
│ The pipeline contains security issues and lacks test coverage.   │
╰──────────────────────────────────────────────────────────────────╯

                        Issues Found
╭──────────┬──────────┬────────────────────┬────────────────────╮
│ Severity │ Category │ Message            │ Suggestion         │
├──────────┼──────────┼────────────────────┼────────────────────┤
│ Error    │ Security │ Hardcoded secrets  │ Use Key Vault or   │
│          │          │                    │ Pipeline variables │
│ Warning  │ Quality  │ No test step       │ Add test step      │
╰──────────┴──────────┴────────────────────┴────────────────────╯
```

## Configuration

Edit `src/DevOpsAiAssistant.Cli/appsettings.json`:

```json
{
  "Ai": {
    "Provider": "GitHubModels",
    "Model": "gpt-4o-mini",
    "ApiKeyEnvironmentVariable": "GITHUB_TOKEN",
    "Endpoint": "https://models.inference.ai.azure.com",
    "DeploymentName": null
  }
}
```

### Provider configurations

**OpenAI:**
```json
{
  "Ai": {
    "Provider": "OpenAI",
    "Model": "gpt-4o-mini",
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY"
  }
}
```

**Ollama (local):**
```json
{
  "Ai": {
    "Provider": "Ollama",
    "Model": "llama3.2",
    "Endpoint": "http://localhost:11434"
  }
}
```

## Project Structure

```
src/
├── DevOpsAiAssistant.Cli/       # CLI application (Spectre.Console)
├── DevOpsAiAssistant.Agents/    # AI integration (Microsoft.Extensions.AI)
└── DevOpsAiAssistant.Domain/    # Core models and abstractions
tests/
├── DevOpsAiAssistant.Agents.Tests/
└── DevOpsAiAssistant.Domain.Tests/
samples/
├── azure-pipelines-good.yml     # Example of a good pipeline
├── azure-pipelines-bad.yml      # Example with issues
└── github-actions-sample.yml    # GitHub Actions example
```

## Technology Stack

- **.NET 10** - Runtime
- **Microsoft.Extensions.AI** - AI abstraction layer
- **Spectre.Console** - Rich CLI output
- **OllamaSharp** - Local LLM support
- **YamlDotNet** - YAML parsing

## Roadmap

- [ ] Add more pipeline platforms (GitLab CI, Jenkins)
- [ ] Implement caching for repeated analyses
- [ ] Add CI/CD integration (GitHub Action, Azure DevOps task)
- [ ] Support for multi-file pipeline analysis
- [ ] Custom rule definitions

## License

MIT
# DevOps AI Assistant - Implementation Plan

## Overview

A CLI tool that uses AI to analyze CI/CD pipeline YAML files, detect issues, and suggest improvements.

**Tech Stack:**
- .NET 10
- Spectre.Console (CLI)
- Microsoft Agent Framework (AI agents) - [GitHub](https://github.com/microsoft/agent-framework)
- Microsoft.Extensions.AI (model abstraction)
- YamlDotNet (YAML parsing)

---

## Phase 1: Solution Scaffolding

### 1.1 Create Solution Structure

```bash
# Create solution directory
mkdir DevOpsAiAssistant
cd DevOpsAiAssistant

# Create solution
dotnet new sln -n DevOpsAiAssistant

# Create projects
dotnet new classlib -n DevOpsAiAssistant.Domain -o src/DevOpsAiAssistant.Domain
dotnet new classlib -n DevOpsAiAssistant.Agents -o src/DevOpsAiAssistant.Agents
dotnet new console -n DevOpsAiAssistant.Cli -o src/DevOpsAiAssistant.Cli

# Create test projects
dotnet new xunit -n DevOpsAiAssistant.Domain.Tests -o tests/DevOpsAiAssistant.Domain.Tests
dotnet new xunit -n DevOpsAiAssistant.Agents.Tests -o tests/DevOpsAiAssistant.Agents.Tests

# Add projects to solution
dotnet sln add src/DevOpsAiAssistant.Domain/DevOpsAiAssistant.Domain.csproj
dotnet sln add src/DevOpsAiAssistant.Agents/DevOpsAiAssistant.Agents.csproj
dotnet sln add src/DevOpsAiAssistant.Cli/DevOpsAiAssistant.Cli.csproj
dotnet sln add tests/DevOpsAiAssistant.Domain.Tests/DevOpsAiAssistant.Domain.Tests.csproj
dotnet sln add tests/DevOpsAiAssistant.Agents.Tests/DevOpsAiAssistant.Agents.Tests.csproj

# Add project references
dotnet add src/DevOpsAiAssistant.Agents/DevOpsAiAssistant.Agents.csproj reference src/DevOpsAiAssistant.Domain/DevOpsAiAssistant.Domain.csproj
dotnet add src/DevOpsAiAssistant.Cli/DevOpsAiAssistant.Cli.csproj reference src/DevOpsAiAssistant.Domain/DevOpsAiAssistant.Domain.csproj
dotnet add src/DevOpsAiAssistant.Cli/DevOpsAiAssistant.Cli.csproj reference src/DevOpsAiAssistant.Agents/DevOpsAiAssistant.Agents.csproj

# Add test references
dotnet add tests/DevOpsAiAssistant.Domain.Tests/DevOpsAiAssistant.Domain.Tests.csproj reference src/DevOpsAiAssistant.Domain/DevOpsAiAssistant.Domain.csproj
dotnet add tests/DevOpsAiAssistant.Agents.Tests/DevOpsAiAssistant.Agents.Tests.csproj reference src/DevOpsAiAssistant.Agents/DevOpsAiAssistant.Agents.csproj
```

### 1.2 Add NuGet Packages

```bash
# Domain - no external dependencies (keep it pure)

# Agents - Using Microsoft Agent Framework (Preview)
dotnet add src/DevOpsAiAssistant.Agents package Microsoft.Agents.AI --prerelease
dotnet add src/DevOpsAiAssistant.Agents package Microsoft.Agents.AI.OpenAI --prerelease
dotnet add src/DevOpsAiAssistant.Agents package Azure.AI.OpenAI --prerelease
dotnet add src/DevOpsAiAssistant.Agents package Azure.Identity
dotnet add src/DevOpsAiAssistant.Agents package Microsoft.Extensions.AI
dotnet add src/DevOpsAiAssistant.Agents package YamlDotNet
dotnet add src/DevOpsAiAssistant.Agents package Microsoft.Extensions.Options.ConfigurationExtensions

# CLI
dotnet add src/DevOpsAiAssistant.Cli package Spectre.Console
dotnet add src/DevOpsAiAssistant.Cli package Spectre.Console.Cli
dotnet add src/DevOpsAiAssistant.Cli package Microsoft.Extensions.Hosting
dotnet add src/DevOpsAiAssistant.Cli package Microsoft.Extensions.Configuration.Json
dotnet add src/DevOpsAiAssistant.Cli package Microsoft.Extensions.Configuration.EnvironmentVariables
dotnet add src/DevOpsAiAssistant.Cli package Microsoft.Extensions.Configuration.UserSecrets

# Tests
dotnet add tests/DevOpsAiAssistant.Agents.Tests package FluentAssertions
dotnet add tests/DevOpsAiAssistant.Domain.Tests package FluentAssertions
```

### 1.3 Final Folder Structure

```
DevOpsAiAssistant/
├── DevOpsAiAssistant.sln
├── src/
│   ├── DevOpsAiAssistant.Domain/
│   │   ├── Enums/
│   │   │   ├── PipelinePlatform.cs
│   │   │   └── IssueSeverity.cs
│   │   ├── Models/
│   │   │   ├── PipelineAnalysisRequest.cs
│   │   │   ├── PipelineAnalysisResult.cs
│   │   │   └── PipelineIssue.cs
│   │   └── Abstractions/
│   │       └── IDevOpsAssistant.cs
│   │
│   ├── DevOpsAiAssistant.Agents/
│   │   ├── Configuration/
│   │   │   └── AiOptions.cs
│   │   ├── Tools/
│   │   │   └── PipelineAnalyzerTools.cs
│   │   ├── Services/
│   │   │   └── DevOpsAssistant.cs
│   │   └── DependencyInjection.cs
│   │
│   └── DevOpsAiAssistant.Cli/
│       ├── Commands/
│       │   ├── AnalyzeCommand.cs
│       │   └── AnalyzeSettings.cs
│       ├── Infrastructure/
│       │   └── TypeRegistrar.cs
│       ├── appsettings.json
│       └── Program.cs
│
├── tests/
│   ├── DevOpsAiAssistant.Domain.Tests/
│   └── DevOpsAiAssistant.Agents.Tests/
│
└── samples/
    ├── azure-pipelines-good.yml
    ├── azure-pipelines-bad.yml
    └── github-actions-sample.yml
```

---

## Phase 2: Domain Layer

### 2.1 Enums

**src/DevOpsAiAssistant.Domain/Enums/PipelinePlatform.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Enums;

public enum PipelinePlatform
{
    AzureDevOps,
    GitHubActions
}
```

**src/DevOpsAiAssistant.Domain/Enums/IssueSeverity.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Enums;

public enum IssueSeverity
{
    Info,
    Warning,
    Error
}
```

### 2.2 Models

**src/DevOpsAiAssistant.Domain/Models/PipelineAnalysisRequest.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Models;

using DevOpsAiAssistant.Domain.Enums;

public sealed record PipelineAnalysisRequest
{
    public required string YamlContent { get; init; }
    public required PipelinePlatform Platform { get; init; }
    public string? FileName { get; init; }
}
```

**src/DevOpsAiAssistant.Domain/Models/PipelineIssue.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Models;

using DevOpsAiAssistant.Domain.Enums;

public sealed record PipelineIssue
{
    public required IssueSeverity Severity { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public string? Suggestion { get; init; }
    public string? LineReference { get; init; }
}
```

**src/DevOpsAiAssistant.Domain/Models/PipelineAnalysisResult.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Models;

public sealed record PipelineAnalysisResult
{
    public required string Summary { get; init; }
    public required IReadOnlyList<PipelineIssue> Issues { get; init; }
    public string? SuggestedYaml { get; init; }
    public PipelineMetadata? Metadata { get; init; }
}

public sealed record PipelineMetadata
{
    public int JobCount { get; init; }
    public int StepCount { get; init; }
    public bool HasTests { get; init; }
    public bool HasCaching { get; init; }
    public bool HasSecurityScanning { get; init; }
    public IReadOnlyList<string> DetectedTools { get; init; } = [];
}
```

### 2.3 Abstractions

**src/DevOpsAiAssistant.Domain/Abstractions/IDevOpsAssistant.cs**
```csharp
namespace DevOpsAiAssistant.Domain.Abstractions;

using DevOpsAiAssistant.Domain.Models;

public interface IDevOpsAssistant
{
    Task<PipelineAnalysisResult> AnalyzePipelineAsync(
        PipelineAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
```

---

## Phase 3: Agents Layer

### 3.1 Configuration

**src/DevOpsAiAssistant.Agents/Configuration/AiOptions.cs**
```csharp
namespace DevOpsAiAssistant.Agents.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "OpenAI"; // OpenAI or AzureOpenAI
    public string Model { get; set; } = "gpt-4o-mini";
    public string? Endpoint { get; set; }
    public string? DeploymentName { get; set; } // For Azure OpenAI
    public string ApiKeyEnvironmentVariable { get; set; } = "OPENAI_API_KEY";
}
```

### 3.2 Function Tools (Microsoft Agent Framework)

Function tools are custom code that the agent can call. Use `AIFunctionFactory.Create` from `Microsoft.Extensions.AI` to create AIFunction instances. Use `System.ComponentModel.DescriptionAttribute` to provide descriptions.

**src/DevOpsAiAssistant.Agents/Tools/PipelineAnalyzerTools.cs**
```csharp
namespace DevOpsAiAssistant.Agents.Tools;

using System.ComponentModel;
using System.Text.Json;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Static methods that will be converted to AI function tools using AIFunctionFactory.Create()
/// </summary>
public static class PipelineAnalyzerTools
{
    [Description("Analyzes the structure of a CI/CD pipeline YAML and returns metadata about jobs, steps, and detected patterns.")]
    public static string AnalyzePipelineStructure(
        [Description("The raw YAML content of the pipeline")] string yamlContent,
        [Description("The platform: AzureDevOps or GitHubActions")] string platform)
    {
        try
        {
            var metadata = ParsePipelineMetadata(yamlContent, platform);
            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [Description("Returns a list of best practices and common issues to check for the given pipeline platform.")]
    public static string GetBestPractices(
        [Description("The platform: AzureDevOps or GitHubActions")] string platform)
    {
        var practices = platform.ToLowerInvariant() switch
        {
            "azuredevops" => GetAzureDevOpsBestPractices(),
            "githubactions" => GetGitHubActionsBestPractices(),
            _ => GetGeneralBestPractices()
        };

        return JsonSerializer.Serialize(practices, new JsonSerializerOptions { WriteIndented = true });
    }

    private static PipelineMetadataDto ParsePipelineMetadata(string yamlContent, string platform)
    {
        var yaml = new YamlStream();
        using var reader = new StringReader(yamlContent);
        yaml.Load(reader);

        var root = (YamlMappingNode)yaml.Documents[0].RootNode;
        var content = yamlContent.ToLowerInvariant();

        return new PipelineMetadataDto
        {
            JobCount = CountJobs(root, platform),
            StepCount = CountSteps(root, platform),
            HasTests = content.Contains("dotnet test") || content.Contains("vstest") || content.Contains("pytest") || content.Contains("npm test"),
            HasCaching = content.Contains("cache@") || content.Contains("actions/cache"),
            HasSecurityScanning = content.Contains("sonar") || content.Contains("owasp") || content.Contains("snyk") || content.Contains("codeql"),
            HasArtifactPublishing = content.Contains("publishbuildartifacts") || content.Contains("upload-artifact"),
            DetectedTools = DetectTools(content)
        };
    }

    private static int CountJobs(YamlMappingNode root, string platform)
    {
        if (root.Children.TryGetValue(new YamlScalarNode("jobs"), out var jobs) && jobs is YamlMappingNode jobsNode)
        {
            return jobsNode.Children.Count;
        }

        // Azure DevOps stages/jobs structure
        if (root.Children.TryGetValue(new YamlScalarNode("stages"), out var stages) && stages is YamlSequenceNode stagesSeq)
        {
            return stagesSeq.Children.Count;
        }

        return 1; // Single job pipeline
    }

    private static int CountSteps(YamlMappingNode root, string platform)
    {
        var count = 0;
        var yaml = root.ToString();
        count = yaml.Split("- task:", StringSplitOptions.None).Length - 1;
        count += yaml.Split("- script:", StringSplitOptions.None).Length - 1;
        count += yaml.Split("- uses:", StringSplitOptions.None).Length - 1;
        count += yaml.Split("- run:", StringSplitOptions.None).Length - 1;
        return Math.Max(count, 1);
    }

    private static List<string> DetectTools(string content)
    {
        var tools = new List<string>();

        if (content.Contains("dotnet")) tools.Add(".NET SDK");
        if (content.Contains("node") || content.Contains("npm")) tools.Add("Node.js");
        if (content.Contains("docker")) tools.Add("Docker");
        if (content.Contains("azure")) tools.Add("Azure CLI");
        if (content.Contains("terraform")) tools.Add("Terraform");
        if (content.Contains("kubectl") || content.Contains("kubernetes")) tools.Add("Kubernetes");
        if (content.Contains("nuget")) tools.Add("NuGet");
        if (content.Contains("sonar")) tools.Add("SonarQube/SonarCloud");

        return tools;
    }

    private static List<BestPractice> GetAzureDevOpsBestPractices() =>
    [
        new("Caching", "Use Cache@2 task to cache NuGet packages and node_modules", "Performance"),
        new("Test Results", "Publish test results using PublishTestResults@2 for visibility", "Quality"),
        new("Code Coverage", "Enable code coverage and publish with PublishCodeCoverageResults@1", "Quality"),
        new("Artifacts", "Use PublishBuildArtifacts@1 to preserve build outputs", "Reliability"),
        new("Variables", "Use variable groups for secrets, never hardcode sensitive values", "Security"),
        new("Templates", "Extract reusable steps into templates for consistency", "Maintainability"),
        new("Triggers", "Configure appropriate branch triggers and PR validation", "Process"),
        new("Pool Selection", "Consider self-hosted agents for sensitive builds or specific requirements", "Security")
    ];

    private static List<BestPractice> GetGitHubActionsBestPractices() =>
    [
        new("Caching", "Use actions/cache to cache dependencies", "Performance"),
        new("Pinned Versions", "Pin action versions to specific SHA or tag, not @main", "Security"),
        new("Secrets", "Use repository or organization secrets, never hardcode", "Security"),
        new("Concurrency", "Use concurrency groups to prevent duplicate runs", "Efficiency"),
        new("Permissions", "Set minimum required permissions using 'permissions' key", "Security"),
        new("Reusable Workflows", "Extract common workflows for reuse across repos", "Maintainability"),
        new("Matrix Builds", "Use matrix strategy for multi-platform/version testing", "Coverage"),
        new("Artifacts", "Use actions/upload-artifact to preserve outputs", "Reliability")
    ];

    private static List<BestPractice> GetGeneralBestPractices() =>
    [
        new("Testing", "Include automated tests in every pipeline", "Quality"),
        new("Security Scanning", "Add SAST/DAST scanning to catch vulnerabilities", "Security"),
        new("Dependency Scanning", "Scan dependencies for known vulnerabilities", "Security"),
        new("Build Caching", "Cache dependencies to speed up builds", "Performance"),
        new("Artifact Management", "Publish and version build artifacts", "Reliability")
    ];

    private sealed record PipelineMetadataDto
    {
        public int JobCount { get; init; }
        public int StepCount { get; init; }
        public bool HasTests { get; init; }
        public bool HasCaching { get; init; }
        public bool HasSecurityScanning { get; init; }
        public bool HasArtifactPublishing { get; init; }
        public List<string> DetectedTools { get; init; } = [];
    }

    private sealed record BestPractice(string Name, string Description, string Category);
}
```

### 3.3 DevOps Assistant Service (Using Microsoft Agent Framework)

**src/DevOpsAiAssistant.Agents/Services/DevOpsAssistant.cs**
```csharp
namespace DevOpsAiAssistant.Agents.Services;

using System.Text.Json;
using DevOpsAiAssistant.Agents.Configuration;
using DevOpsAiAssistant.Agents.Tools;
using DevOpsAiAssistant.Domain.Abstractions;
using DevOpsAiAssistant.Domain.Enums;
using DevOpsAiAssistant.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI;

public sealed class DevOpsAssistant : IDevOpsAssistant
{
    private readonly AIAgent _agent;
    private readonly ILogger<DevOpsAssistant> _logger;

    public DevOpsAssistant(AIAgent agent, ILogger<DevOpsAssistant> logger)
    {
        _agent = agent;
        _logger = logger;
    }

    public async Task<PipelineAnalysisResult> AnalyzePipelineAsync(
        PipelineAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing pipeline for platform {Platform}", request.Platform);

        var userPrompt = BuildUserPrompt(request);

        try
        {
            // Use the AIAgent to run the analysis - it will automatically use the configured tools
            var response = await _agent.RunAsync(userPrompt, cancellationToken);

            var result = ParseResponse(response ?? "{}");

            _logger.LogInformation("Analysis complete. Found {IssueCount} issues", result.Issues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze pipeline");
            throw;
        }
    }

    private static string BuildUserPrompt(PipelineAnalysisRequest request) => $"""
        Please analyze this {request.Platform} pipeline:

        Filename: {request.FileName ?? "pipeline.yml"}

        ```yaml
        {request.YamlContent}
        ```

        Use your tools to analyze the structure and check against best practices, then provide your analysis as JSON.
        """;

    private static PipelineAnalysisResult ParseResponse(string jsonResponse)
    {
        try
        {
            // Clean up response if wrapped in markdown
            var json = jsonResponse
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            var response = JsonSerializer.Deserialize<AgentResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (response is null)
            {
                return CreateErrorResult("Failed to parse AI response");
            }

            return new PipelineAnalysisResult
            {
                Summary = response.Summary ?? "Analysis complete",
                Issues = response.Issues?.Select(i => new PipelineIssue
                {
                    Severity = Enum.TryParse<IssueSeverity>(i.Severity, true, out var sev) ? sev : IssueSeverity.Info,
                    Category = i.Category ?? "General",
                    Message = i.Message ?? "",
                    Suggestion = i.Suggestion,
                    LineReference = i.LineReference
                }).ToList() ?? [],
                SuggestedYaml = response.SuggestedYaml,
                Metadata = response.Metadata is not null ? new PipelineMetadata
                {
                    JobCount = response.Metadata.JobCount,
                    StepCount = response.Metadata.StepCount,
                    HasTests = response.Metadata.HasTests,
                    HasCaching = response.Metadata.HasCaching,
                    HasSecurityScanning = response.Metadata.HasSecurityScanning,
                    DetectedTools = response.Metadata.DetectedTools ?? []
                } : null
            };
        }
        catch (JsonException ex)
        {
            return CreateErrorResult($"JSON parsing error: {ex.Message}");
        }
    }

    private static PipelineAnalysisResult CreateErrorResult(string message) => new()
    {
        Summary = message,
        Issues =
        [
            new PipelineIssue
            {
                Severity = IssueSeverity.Error,
                Category = "System",
                Message = message,
                Suggestion = "Please try again or check the pipeline YAML syntax"
            }
        ]
    };

    // DTOs for JSON deserialization
    private sealed record AgentResponse
    {
        public string? Summary { get; init; }
        public List<IssueDto>? Issues { get; init; }
        public string? SuggestedYaml { get; init; }
        public MetadataDto? Metadata { get; init; }
    }

    private sealed record IssueDto
    {
        public string? Severity { get; init; }
        public string? Category { get; init; }
        public string? Message { get; init; }
        public string? Suggestion { get; init; }
        public string? LineReference { get; init; }
    }

    private sealed record MetadataDto
    {
        public int JobCount { get; init; }
        public int StepCount { get; init; }
        public bool HasTests { get; init; }
        public bool HasCaching { get; init; }
        public bool HasSecurityScanning { get; init; }
        public List<string>? DetectedTools { get; init; }
    }
}
```

### 3.4 Dependency Injection (Microsoft Agent Framework)

**src/DevOpsAiAssistant.Agents/DependencyInjection.cs**
```csharp
namespace DevOpsAiAssistant.Agents;

using Azure.AI.OpenAI;
using Azure.Identity;
using DevOpsAiAssistant.Agents.Configuration;
using DevOpsAiAssistant.Agents.Tools;
using DevOpsAiAssistant.Agents.Services;
using DevOpsAiAssistant.Domain.Abstractions;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

public static class DependencyInjection
{
    private const string SystemPrompt = """
        You are a senior DevOps engineer specializing in CI/CD pipelines for .NET applications.

        Your task is to analyze pipeline YAML files and provide actionable feedback.

        You have access to these tools:
        - AnalyzePipelineStructure: Use this FIRST to understand the pipeline structure
        - GetBestPractices: Use this to get platform-specific best practices to check against

        ALWAYS use both tools before providing your analysis.

        Your response MUST be a valid JSON object with this exact structure:
        {
            "summary": "A 2-3 sentence overview of what the pipeline does",
            "issues": [
                {
                    "severity": "Error|Warning|Info",
                    "category": "Security|Performance|Quality|Reliability|Maintainability",
                    "message": "Clear description of the issue",
                    "suggestion": "Specific actionable fix",
                    "lineReference": "Optional: relevant YAML snippet or line hint"
                }
            ],
            "suggestedYaml": "The complete improved YAML (or null if no changes needed)",
            "metadata": {
                "jobCount": 0,
                "stepCount": 0,
                "hasTests": false,
                "hasCaching": false,
                "hasSecurityScanning": false,
                "detectedTools": []
            }
        }

        Guidelines:
        - Be specific and actionable in suggestions
        - Severity levels: Error = must fix, Warning = should fix, Info = nice to have
        - If suggesting YAML changes, provide the COMPLETE working YAML, not fragments
        - Focus on .NET-specific best practices when applicable
        - Do not invent issues - only report real problems found

        Respond ONLY with the JSON object, no markdown formatting or explanation.
        """;

    public static IServiceCollection AddAgents(this IServiceCollection services, IConfiguration configuration)
    {
        var aiOptions = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>()
            ?? new AiOptions();

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Get API key from environment variable
        var apiKey = Environment.GetEnvironmentVariable(aiOptions.ApiKeyEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"API key not found. Set the '{aiOptions.ApiKeyEnvironmentVariable}' environment variable.");

        // Create function tools from our static methods using AIFunctionFactory
        var tools = new AIFunction[]
        {
            AIFunctionFactory.Create(PipelineAnalyzerTools.AnalyzePipelineStructure),
            AIFunctionFactory.Create(PipelineAnalyzerTools.GetBestPractices)
        };

        // Build AIAgent using Microsoft Agent Framework
        AIAgent agent;

        if (aiOptions.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            // Azure OpenAI configuration
            var azureClient = new AzureOpenAIClient(
                new Uri(aiOptions.Endpoint ?? throw new InvalidOperationException("Azure OpenAI endpoint is required")),
                new Azure.AzureKeyCredential(apiKey));

            agent = azureClient
                .GetChatClient(aiOptions.DeploymentName ?? aiOptions.Model)
                .CreateAIAgent(
                    name: "DevOpsPipelineAnalyzer",
                    instructions: SystemPrompt,
                    tools: tools);
        }
        else
        {
            // OpenAI configuration
            var openAiClient = new OpenAIClient(apiKey);

            agent = openAiClient
                .GetChatClient(aiOptions.Model)
                .CreateAIAgent(
                    name: "DevOpsPipelineAnalyzer",
                    instructions: SystemPrompt,
                    tools: tools);
        }

        services.AddSingleton(agent);
        services.AddScoped<IDevOpsAssistant, DevOpsAssistant>();

        return services;
    }
}
```

---

## Phase 4: CLI Layer

### 4.1 Command Settings

**src/DevOpsAiAssistant.Cli/Commands/AnalyzeSettings.cs**
```csharp
namespace DevOpsAiAssistant.Cli.Commands;

using System.ComponentModel;
using DevOpsAiAssistant.Domain.Enums;
using Spectre.Console.Cli;

public sealed class AnalyzeSettings : CommandSettings
{
    [CommandOption("-f|--file <FILE>")]
    [Description("Path to the pipeline YAML file")]
    public string? FilePath { get; init; }

    [CommandOption("-p|--platform <PLATFORM>")]
    [Description("Pipeline platform: AzureDevOps or GitHubActions")]
    [DefaultValue(PipelinePlatform.AzureDevOps)]
    public PipelinePlatform Platform { get; init; } = PipelinePlatform.AzureDevOps;

    [CommandOption("--format <FORMAT>")]
    [Description("Output format: table or json")]
    [DefaultValue("table")]
    public string Format { get; init; } = "table";

    [CommandOption("--show-yaml")]
    [Description("Display the suggested improved YAML")]
    public bool ShowYaml { get; init; }

    [CommandOption("--output <FILE>")]
    [Description("Save suggested YAML to a file")]
    public string? OutputPath { get; init; }

    [CommandOption("--demo")]
    [Description("Run with a built-in sample pipeline")]
    public bool Demo { get; init; }
}
```

### 4.2 Analyze Command

**src/DevOpsAiAssistant.Cli/Commands/AnalyzeCommand.cs**
```csharp
namespace DevOpsAiAssistant.Cli.Commands;

using System.Text.Json;
using DevOpsAiAssistant.Domain.Abstractions;
using DevOpsAiAssistant.Domain.Enums;
using DevOpsAiAssistant.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class AnalyzeCommand : AsyncCommand<AnalyzeSettings>
{
    private readonly IDevOpsAssistant _assistant;

    public AnalyzeCommand(IDevOpsAssistant assistant)
    {
        _assistant = assistant;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AnalyzeSettings settings)
    {
        try
        {
            var (yamlContent, fileName) = await GetYamlContentAsync(settings);

            if (string.IsNullOrWhiteSpace(yamlContent))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] No pipeline content to analyze.");
                return 1;
            }

            var request = new PipelineAnalysisRequest
            {
                YamlContent = yamlContent,
                Platform = settings.Platform,
                FileName = fileName
            };

            PipelineAnalysisResult result;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Analyzing pipeline...", async ctx =>
                {
                    ctx.Status("Parsing pipeline structure...");
                    await Task.Delay(500); // Visual feedback

                    ctx.Status("Checking best practices...");
                    result = await _assistant.AnalyzePipelineAsync(request);
                });

            // Re-fetch result outside status context
            result = await _assistant.AnalyzePipelineAsync(request);

            if (settings.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                OutputJson(result);
            }
            else
            {
                OutputTable(result, fileName, settings);
            }

            if (!string.IsNullOrWhiteSpace(settings.OutputPath) && !string.IsNullOrWhiteSpace(result.SuggestedYaml))
            {
                await File.WriteAllTextAsync(settings.OutputPath, result.SuggestedYaml);
                AnsiConsole.MarkupLine($"\n[green]✓[/] Suggested YAML saved to: [blue]{settings.OutputPath}[/]");
            }

            return result.Issues.Any(i => i.Severity == IssueSeverity.Error) ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<(string Content, string FileName)> GetYamlContentAsync(AnalyzeSettings settings)
    {
        if (settings.Demo)
        {
            return (GetDemoYaml(settings.Platform), "demo-pipeline.yml");
        }

        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            AnsiConsole.MarkupLine("[yellow]No file specified.[/] Use --file <path> or --demo");
            return (string.Empty, string.Empty);
        }

        if (!File.Exists(settings.FilePath))
        {
            AnsiConsole.MarkupLine($"[red]File not found:[/] {settings.FilePath}");
            return (string.Empty, string.Empty);
        }

        var content = await File.ReadAllTextAsync(settings.FilePath);
        return (content, Path.GetFileName(settings.FilePath));
    }

    private static void OutputTable(PipelineAnalysisResult result, string fileName, AnalyzeSettings settings)
    {
        // Header panel
        var header = new Panel($"[bold]{fileName}[/] | Platform: [blue]{settings.Platform}[/]")
            .Border(BoxBorder.Rounded)
            .Header("[bold blue]Pipeline Analysis[/]");
        AnsiConsole.Write(header);
        AnsiConsole.WriteLine();

        // Metadata panel (if available)
        if (result.Metadata is not null)
        {
            var metaTable = new Table().Border(TableBorder.None).HideHeaders();
            metaTable.AddColumn("");
            metaTable.AddColumn("");
            metaTable.AddRow("Jobs:", result.Metadata.JobCount.ToString());
            metaTable.AddRow("Steps:", result.Metadata.StepCount.ToString());
            metaTable.AddRow("Has Tests:", result.Metadata.HasTests ? "[green]Yes[/]" : "[red]No[/]");
            metaTable.AddRow("Has Caching:", result.Metadata.HasCaching ? "[green]Yes[/]" : "[red]No[/]");
            metaTable.AddRow("Security Scanning:", result.Metadata.HasSecurityScanning ? "[green]Yes[/]" : "[yellow]No[/]");

            if (result.Metadata.DetectedTools.Count > 0)
            {
                metaTable.AddRow("Tools:", string.Join(", ", result.Metadata.DetectedTools));
            }

            AnsiConsole.Write(new Panel(metaTable).Header("[bold]Metadata[/]").Border(BoxBorder.Rounded));
            AnsiConsole.WriteLine();
        }

        // Summary panel
        AnsiConsole.Write(new Panel(result.Summary)
            .Header("[bold]Summary[/]")
            .Border(BoxBorder.Rounded));
        AnsiConsole.WriteLine();

        // Issues table
        if (result.Issues.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Issues Found[/]")
                .AddColumn("Severity")
                .AddColumn("Category")
                .AddColumn("Message")
                .AddColumn("Suggestion");

            foreach (var issue in result.Issues.OrderByDescending(i => i.Severity))
            {
                var severityMarkup = issue.Severity switch
                {
                    IssueSeverity.Error => "[red]Error[/]",
                    IssueSeverity.Warning => "[yellow]Warning[/]",
                    IssueSeverity.Info => "[grey]Info[/]",
                    _ => issue.Severity.ToString()
                };

                table.AddRow(
                    severityMarkup,
                    $"[blue]{issue.Category}[/]",
                    Markup.Escape(issue.Message),
                    Markup.Escape(issue.Suggestion ?? "-"));
            }

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]✓ No issues found![/]");
        }

        // Suggested YAML
        if (settings.ShowYaml && !string.IsNullOrWhiteSpace(result.SuggestedYaml))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(Markup.Escape(result.SuggestedYaml))
                .Header("[bold green]Suggested YAML[/]")
                .Border(BoxBorder.Rounded)
                .Expand());
        }
    }

    private static void OutputJson(PipelineAnalysisResult result)
    {
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        Console.WriteLine(json);
    }

    private static string GetDemoYaml(PipelinePlatform platform) => platform switch
    {
        PipelinePlatform.AzureDevOps => """
            trigger:
              - main

            pool:
              vmImage: 'ubuntu-latest'

            steps:
              - task: DotNetCoreCLI@2
                displayName: 'Restore packages'
                inputs:
                  command: 'restore'
                  projects: '**/*.csproj'

              - task: DotNetCoreCLI@2
                displayName: 'Build'
                inputs:
                  command: 'build'
                  projects: '**/*.csproj'
                  arguments: '--configuration Release'

              - script: echo 'Deployment would happen here'
                displayName: 'Deploy placeholder'
            """,

        PipelinePlatform.GitHubActions => """
            name: Build and Test

            on:
              push:
                branches: [ main ]
              pull_request:
                branches: [ main ]

            jobs:
              build:
                runs-on: ubuntu-latest

                steps:
                - uses: actions/checkout@main

                - name: Setup .NET
                  uses: actions/setup-dotnet@v3
                  with:
                    dotnet-version: 10.0.x

                - name: Restore
                  run: dotnet restore

                - name: Build
                  run: dotnet build --configuration Release
            """,

        _ => throw new ArgumentOutOfRangeException(nameof(platform))
    };
}
```

### 4.3 Spectre DI Integration

**src/DevOpsAiAssistant.Cli/Infrastructure/TypeRegistrar.cs**
```csharp
namespace DevOpsAiAssistant.Cli.Infrastructure;

using Spectre.Console.Cli;

public sealed class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceProvider _serviceProvider;

    public TypeRegistrar(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ITypeResolver Build() => new TypeResolver(_serviceProvider);

    public void Register(Type service, Type implementation)
    {
        // Not needed - we use the existing DI container
    }

    public void RegisterInstance(Type service, object implementation)
    {
        // Not needed - we use the existing DI container
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        // Not needed - we use the existing DI container
    }
}

public sealed class TypeResolver : ITypeResolver
{
    private readonly IServiceProvider _serviceProvider;

    public TypeResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? Resolve(Type? type)
    {
        return type is null ? null : _serviceProvider.GetService(type);
    }
}
```

### 4.4 Program Entry Point

**src/DevOpsAiAssistant.Cli/Program.cs**
```csharp
using DevOpsAiAssistant.Agents;
using DevOpsAiAssistant.Cli.Commands;
using DevOpsAiAssistant.Cli.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

// Logging
builder.Logging
    .ClearProviders()
    .AddFilter("Microsoft", LogLevel.Warning)
    .AddFilter("System", LogLevel.Warning);

// Services
builder.Services.AddAgents(builder.Configuration);

// Build host
var host = builder.Build();

// Configure Spectre CLI
var registrar = new TypeRegistrar(host.Services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("devops-ai");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<AnalyzeCommand>("analyze")
        .WithDescription("Analyze a CI/CD pipeline YAML file")
        .WithExample("analyze", "--file", "azure-pipelines.yml")
        .WithExample("analyze", "--file", "azure-pipelines.yml", "--show-yaml")
        .WithExample("analyze", "--demo", "--platform", "GitHubActions");
});

return await app.RunAsync(args);
```

### 4.5 Configuration File

**src/DevOpsAiAssistant.Cli/appsettings.json**
```json
{
  "Ai": {
    "Provider": "OpenAI",
    "Model": "gpt-4o-mini",
    "ApiKeyEnvironmentVariable": "OPENAI_API_KEY"
  }
}
```

---

## Phase 5: Sample Files

### 5.1 Good Azure Pipeline

**samples/azure-pipelines-good.yml**
```yaml
trigger:
  branches:
    include:
      - main
      - release/*

pr:
  branches:
    include:
      - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '10.0.x'

stages:
  - stage: Build
    displayName: 'Build & Test'
    jobs:
      - job: BuildJob
        displayName: 'Build'
        steps:
          - task: UseDotNet@2
            displayName: 'Install .NET SDK'
            inputs:
              version: $(dotnetVersion)

          - task: Cache@2
            displayName: 'Cache NuGet packages'
            inputs:
              key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
              restoreKeys: |
                nuget | "$(Agent.OS)"
              path: $(Pipeline.Workspace)/.nuget/packages

          - task: DotNetCoreCLI@2
            displayName: 'Restore'
            inputs:
              command: 'restore'
              projects: '**/*.csproj'
              feedsToUse: 'select'

          - task: DotNetCoreCLI@2
            displayName: 'Build'
            inputs:
              command: 'build'
              projects: '**/*.csproj'
              arguments: '--configuration $(buildConfiguration) --no-restore'

          - task: DotNetCoreCLI@2
            displayName: 'Test'
            inputs:
              command: 'test'
              projects: '**/*Tests.csproj'
              arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'

          - task: PublishCodeCoverageResults@2
            displayName: 'Publish Code Coverage'
            inputs:
              summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

          - task: PublishBuildArtifacts@1
            displayName: 'Publish Artifacts'
            inputs:
              PathtoPublish: '$(Build.ArtifactStagingDirectory)'
              ArtifactName: 'drop'
```

### 5.2 Bad Azure Pipeline

**samples/azure-pipelines-bad.yml**
```yaml
# Missing trigger configuration - will trigger on all branches
pool:
  vmImage: 'windows-latest'  # More expensive than ubuntu

steps:
  - script: dotnet restore
    displayName: 'Restore'

  - script: dotnet build
    displayName: 'Build'
    # Missing --configuration, no --no-restore

  # No test step at all!

  # Hardcoded secrets (BAD!)
  - script: |
      echo "Deploying with password: P@ssw0rd123"
    displayName: 'Deploy'
    env:
      API_KEY: 'sk-1234567890abcdef'  # Exposed secret!

  # No artifact publishing
  # No caching
  # No code coverage
```

### 5.3 GitHub Actions Sample

**samples/github-actions-sample.yml**
```yaml
name: Build

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@main  # Should pin version

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 10.0.x

    - name: Build
      run: dotnet build

    # Missing: restore step, tests, caching, artifacts
```

---

## Quick Start Checklist

```
[ ] Phase 1: Create solution structure (copy bash commands)
[ ] Phase 2: Add domain models (copy all .cs files)
[ ] Phase 3: Add agents layer (copy configuration, plugin, service, DI)
[ ] Phase 4: Add CLI layer (copy commands, infrastructure, program, config)
[ ] Phase 5: Add sample files
[ ] Phase 6: Add README
[ ] Set OPENAI_API_KEY environment variable
[ ] Run: dotnet build
[ ] Test: dotnet run --project src/DevOpsAiAssistant.Cli -- analyze --demo
```

---

## Future Enhancements (v2+)

- [ ] Interactive mode with follow-up questions
- [ ] Multi-file analysis (templates, variable groups)
- [ ] Git integration (analyze repo's pipeline)
- [ ] Custom rules configuration
- [ ] HTML report generation
- [ ] VS Code extension
- [ ] Pre-commit hook integration

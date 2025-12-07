namespace DevOpsAiAssistant.Agents.Services;

using System.Text.Json;
using DevOpsAiAssistant.Domain.Abstractions;
using DevOpsAiAssistant.Domain.Enums;
using DevOpsAiAssistant.Domain.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

public sealed class DevOpsAssistant : IDevOpsAssistant
{
    private const string SystemPrompt = """
        You are a senior DevOps engineer specializing in CI/CD pipelines for .NET applications.
        Your task is to analyze pipeline YAML files and provide actionable feedback.
        Your response MUST be a valid JSON object. Do not include markdown formatting.
        Severity levels: Error = must fix, Warning = should fix, Info = nice to have.
        Be specific and actionable in suggestions.
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IChatClient _chatClient;
    private readonly ILogger<DevOpsAssistant> _logger;

    public DevOpsAssistant(IChatClient chatClient, ILogger<DevOpsAssistant> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<PipelineAnalysisResult> AnalyzePipelineAsync(
        PipelineAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing pipeline for platform {Platform}", request.Platform);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, BuildUserPrompt(request))
        };

        try
        {
            var response = await _chatClient.GetResponseAsync(messages, cancellationToken: cancellationToken);

            var result = ParseResponse(response.Text ?? "{}");

            _logger.LogInformation("Analysis complete. Found {IssueCount} issues", result.Issues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze pipeline");
            throw;
        }
    }

    private static string BuildUserPrompt(PipelineAnalysisRequest request) => $$"""
        Please analyze this {{request.Platform}} pipeline:

        Filename: {{request.FileName ?? "pipeline.yml"}}

        ```yaml
        {{request.YamlContent}}
        ```

        Provide your analysis as JSON with this structure:
        {
            "summary": "A 2-3 sentence overview",
            "issues": [
                {
                    "severity": "Error|Warning|Info",
                    "category": "Security|Performance|Quality|Reliability|Maintainability",
                    "message": "Description",
                    "suggestion": "Fix",
                    "lineReference": "Optional hint"
                }
            ],
            "suggestedYaml": "Improved YAML or null",
            "metadata": {
                "jobCount": 0,
                "stepCount": 0,
                "hasTests": false,
                "hasCaching": false,
                "hasSecurityScanning": false,
                "hasArtifactPublishing": false,
                "detectedTools": []
            }
        }
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

            var response = JsonSerializer.Deserialize<AgentResponse>(json, JsonOptions);

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
                    HasArtifactPublishing = response.Metadata.HasArtifactPublishing,
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
        public bool HasArtifactPublishing { get; init; }
        public List<string>? DetectedTools { get; init; }
    }
}

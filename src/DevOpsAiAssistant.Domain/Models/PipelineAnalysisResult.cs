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
    public bool HasArtifactPublishing { get; init; }
    public IReadOnlyList<string> DetectedTools { get; init; } = [];
}

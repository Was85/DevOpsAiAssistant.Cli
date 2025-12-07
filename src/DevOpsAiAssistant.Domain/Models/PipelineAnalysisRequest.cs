namespace DevOpsAiAssistant.Domain.Models;

using DevOpsAiAssistant.Domain.Enums;

public sealed record PipelineAnalysisRequest
{
    public required string YamlContent { get; init; }
    public required PipelinePlatform Platform { get; init; }
    public string? FileName { get; init; }
}

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

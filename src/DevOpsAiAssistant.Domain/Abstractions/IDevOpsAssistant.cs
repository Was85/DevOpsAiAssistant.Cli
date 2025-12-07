namespace DevOpsAiAssistant.Domain.Abstractions;

using DevOpsAiAssistant.Domain.Models;

public interface IDevOpsAssistant
{
    Task<PipelineAnalysisResult> AnalyzePipelineAsync(
        PipelineAnalysisRequest request,
        CancellationToken cancellationToken = default);
}

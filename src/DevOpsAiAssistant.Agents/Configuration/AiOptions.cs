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

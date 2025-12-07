namespace DevOpsAiAssistant.Agents.Tools;

using System.ComponentModel;
using System.Text.Json;
using YamlDotNet.RepresentationModel;

/// <summary>
/// Static methods that will be converted to AI function tools using AIFunctionFactory.Create()
/// </summary>
public static class PipelineAnalyzerTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Description("Analyzes the structure of a CI/CD pipeline YAML and returns metadata about jobs, steps, and detected patterns.")]
    public static string AnalyzePipelineStructure(
        [Description("The raw YAML content of the pipeline")] string yamlContent,
        [Description("The platform: AzureDevOps or GitHubActions")] string platform)
    {
        try
        {
            var metadata = ParsePipelineMetadata(yamlContent, platform);
            return JsonSerializer.Serialize(metadata, JsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
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

        return JsonSerializer.Serialize(practices, JsonOptions);
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
            JobCount = CountJobs(root),
            StepCount = CountSteps(yamlContent),
            HasTests = content.Contains("dotnet test") || content.Contains("vstest") || content.Contains("pytest") || content.Contains("npm test"),
            HasCaching = content.Contains("cache@") || content.Contains("actions/cache"),
            HasSecurityScanning = content.Contains("sonar") || content.Contains("owasp") || content.Contains("snyk") || content.Contains("codeql"),
            HasArtifactPublishing = content.Contains("publishbuildartifacts") || content.Contains("upload-artifact"),
            DetectedTools = DetectTools(content)
        };
    }

    private static int CountJobs(YamlMappingNode root)
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

    private static int CountSteps(string yamlContent)
    {
        var count = 0;
        count = yamlContent.Split("- task:", StringSplitOptions.None).Length - 1;
        count += yamlContent.Split("- script:", StringSplitOptions.None).Length - 1;
        count += yamlContent.Split("- uses:", StringSplitOptions.None).Length - 1;
        count += yamlContent.Split("- run:", StringSplitOptions.None).Length - 1;
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

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

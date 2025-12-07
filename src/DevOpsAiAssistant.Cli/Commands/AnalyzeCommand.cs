namespace DevOpsAiAssistant.Cli.Commands;

using System.Text.Json;
using DevOpsAiAssistant.Domain.Abstractions;
using DevOpsAiAssistant.Domain.Enums;
using DevOpsAiAssistant.Domain.Models;
using Spectre.Console;
using Spectre.Console.Cli;

public sealed class AnalyzeCommand : AsyncCommand<AnalyzeSettings>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDevOpsAssistant _assistant;

    public AnalyzeCommand(IDevOpsAssistant assistant)
    {
        _assistant = assistant;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AnalyzeSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var (yamlContent, fileName) = await GetYamlContentAsync(settings, cancellationToken);

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

            PipelineAnalysisResult result = null!;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Analyzing pipeline...", async ctx =>
                {
                    ctx.Status("Checking best practices...");
                    result = await _assistant.AnalyzePipelineAsync(request, cancellationToken);
                });

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
                await File.WriteAllTextAsync(settings.OutputPath, result.SuggestedYaml, cancellationToken);
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

    private static async Task<(string Content, string FileName)> GetYamlContentAsync(AnalyzeSettings settings, CancellationToken cancellationToken)
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

        var content = await File.ReadAllTextAsync(settings.FilePath, cancellationToken);
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
            metaTable.AddRow("Artifact Publishing:", result.Metadata.HasArtifactPublishing ? "[green]Yes[/]" : "[yellow]No[/]");

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
        var json = JsonSerializer.Serialize(result, JsonOptions);
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
                    dotnet-version: 9.0.x

                - name: Restore
                  run: dotnet restore

                - name: Build
                  run: dotnet build --configuration Release
            """,

        _ => throw new ArgumentOutOfRangeException(nameof(platform))
    };
}

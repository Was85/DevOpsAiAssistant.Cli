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

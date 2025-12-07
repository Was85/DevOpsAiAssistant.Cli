namespace DevOpsAiAssistant.Agents;

using Azure.AI.OpenAI;
using DevOpsAiAssistant.Agents.Configuration;
using DevOpsAiAssistant.Agents.Services;
using DevOpsAiAssistant.Domain.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

public static class DependencyInjection
{
    public static IServiceCollection AddAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

        // Register chat client factory - deferred creation until actually needed
        services.AddSingleton<IChatClient>(sp =>
        {
            var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>().Value;

            return aiOptions.Provider.ToLowerInvariant() switch
            {
                "azureopenai" => CreateAzureOpenAIClient(aiOptions),
                "ollama" => CreateOllamaClient(aiOptions),
                "githubmodels" => CreateGitHubModelsClient(aiOptions),
                _ => CreateOpenAIClient(aiOptions) // Default to OpenAI
            };
        });

        services.AddScoped<IDevOpsAssistant, DevOpsAssistant>();

        return services;
    }

    private static IChatClient CreateOpenAIClient(AiOptions options)
    {
        var apiKey = GetRequiredApiKey(options);
        return new ChatClient(options.Model, apiKey).AsIChatClient();
    }

    private static IChatClient CreateAzureOpenAIClient(AiOptions options)
    {
        var apiKey = GetRequiredApiKey(options);
        var endpoint = options.Endpoint
            ?? throw new InvalidOperationException("Azure OpenAI endpoint is required. Set 'AI:Endpoint' in configuration.");

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new System.ClientModel.ApiKeyCredential(apiKey));

        return azureClient
            .GetChatClient(options.DeploymentName ?? options.Model)
            .AsIChatClient();
    }

    private static IChatClient CreateOllamaClient(AiOptions options)
    {
        var endpoint = options.Endpoint ?? "http://localhost:11434";
        return new OllamaSharp.OllamaApiClient(new Uri(endpoint), options.Model);
    }

    private static IChatClient CreateGitHubModelsClient(AiOptions options)
    {
        var apiKey = GetRequiredApiKey(options);
        var endpoint = options.Endpoint ?? "https://models.inference.ai.azure.com";

        var openAiClient = new OpenAIClient(
            new System.ClientModel.ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

        return openAiClient
            .GetChatClient(options.Model)
            .AsIChatClient();
    }

    private static string GetRequiredApiKey(AiOptions options)
    {
        return Environment.GetEnvironmentVariable(options.ApiKeyEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"API key not found. Set the '{options.ApiKeyEnvironmentVariable}' environment variable.");
    }
}

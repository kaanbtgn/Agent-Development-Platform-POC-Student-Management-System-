using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using StudentManagement.Agent.Services;

namespace StudentManagement.Agent.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAgentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Azure Document Intelligence (opsiyonel — yalnızca dosya yüklendiğinde kullanılır) ──
        var docIntelEndpoint = configuration["AzureDocumentIntelligence:Endpoint"];
        var docIntelKey = configuration["AzureDocumentIntelligence:ApiKey"];

        if (!string.IsNullOrWhiteSpace(docIntelEndpoint)
            && !string.IsNullOrWhiteSpace(docIntelKey)
            && Uri.TryCreate(docIntelEndpoint, UriKind.Absolute, out var docIntelUri)
            && !docIntelUri.Host.Contains('<'))  // placeholder guard
        {
            services.AddSingleton(_ => new DocumentIntelligenceClient(
                docIntelUri,
                new AzureKeyCredential(docIntelKey)));

            services.AddSingleton<AzureDocumentIntelligenceService>();
        }

        // ── McpClient — HTTP transport üzerinden MCP Server'a bağlanır ─
        // Lazy<T> ile sarılıyor: DI registration anında değil, ilk kullanımda bağlantı kurulur
        services.AddSingleton<Lazy<Task<McpClient>>>(_ =>
        {
            var mcpUrl = configuration["Mcp:ServerUrl"]
                ?? throw new InvalidOperationException("Mcp:ServerUrl eksik.");

            return new Lazy<Task<McpClient>>(() =>
            {
                var transport = new HttpClientTransport(new HttpClientTransportOptions
                {
                    Endpoint = new Uri(mcpUrl.TrimEnd('/') + "/mcp"),
                });
                return McpClient.CreateAsync(transport);
            });
        });

        // ── IChatClient — Azure OpenAI + FunctionInvocation middleware ──
        var openAiEndpoint = configuration["AzureOpenAI:Endpoint"];
        var openAiKey = configuration["AzureOpenAI:ApiKey"];
        var deploymentName = configuration["AzureOpenAI:DeploymentName"];

        if (!string.IsNullOrWhiteSpace(openAiEndpoint) && !string.IsNullOrWhiteSpace(openAiKey) && !string.IsNullOrWhiteSpace(deploymentName))
        {
            services.AddSingleton<IChatClient>(_ =>
                new AzureOpenAIClient(
                    new Uri(openAiEndpoint),
                    new AzureKeyCredential(openAiKey))
                .GetChatClient(deploymentName)
                .AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation(configure: client =>
                {
                    client.MaximumIterationsPerRequest = 3;
                    client.MaximumConsecutiveErrorsPerRequest = 3;
                })
                .Build());
        }
        // ── Orkestratör ─────────────────────────────────────────────────
        services.AddSingleton<StudentManagementAgent>();

        return services;
    }
}

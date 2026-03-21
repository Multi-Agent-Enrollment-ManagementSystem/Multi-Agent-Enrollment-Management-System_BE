using MAEMS.Application.Interfaces;
using MAEMS.MultiAgent.Agents;
using MAEMS.MultiAgent.RAG.Interfaces;
using MAEMS.MultiAgent.RAG.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MAEMS.MultiAgent;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký tất cả các Multi-Agent services vào DI container.
    /// </summary>
    public static IServiceCollection AddMultiAgentServices(this IServiceCollection services, IConfiguration configuration)
    {
        var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 300);

        // DocumentIntakeAgent — quality check on upload
        services.AddHttpClient<IDocumentIntakeAgent, DocumentIntakeAgent>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        // EligibilityEvaluationAgent — check document completeness + profile quality
        // Registered BEFORE DocumentVerificationAgent so it can be injected into it
        services.AddHttpClient<IEligibilityEvaluationAgent, EligibilityEvaluationAgent>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        // DocumentVerificationAgent — cross-check documents on submission (fire-and-forget)
        // Depends on IEligibilityEvaluationAgent
        services.AddHttpClient<IDocumentVerificationAgent, DocumentVerificationAgent>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        // ChatBoxAgent — handle Q&A about admission requirements
        // Uses Application.Interfaces.IChatBoxAgent interface (not the one in MultiAgent.Agents)
        services.AddScoped<IChatBoxAgent, ChatBoxAgent>();

        // RAG Services - Use factory to create scoped instances from singleton BackgroundService
        services.AddScoped<IRagDocumentLoader, RagDocumentLoader>();

        // Embedding cache service - singleton to persist across requests
        services.AddSingleton<IEmbeddingCacheService, EmbeddingCacheService>();

        services.AddHttpClient<IRagEmbeddingService, RagEmbeddingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });
        services.AddHttpClient<IRagVectorStore, RagVectorStore>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<IRagRetrievalService, RagRetrievalService>();
        services.AddHostedService<RagInitializerService>();

        return services;
    }
}

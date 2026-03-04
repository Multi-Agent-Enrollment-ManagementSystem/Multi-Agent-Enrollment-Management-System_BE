using MAEMS.Application.Interfaces;
using MAEMS.MultiAgent.Agents;
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
        // Register named HttpClient for DocumentIntakeAgent with timeout configuration
        services.AddHttpClient<IDocumentIntakeAgent, DocumentIntakeAgent>(client =>
        {
            // LLM calls can be slow — set a generous timeout (default is 100 s)
            var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 120);
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }
}

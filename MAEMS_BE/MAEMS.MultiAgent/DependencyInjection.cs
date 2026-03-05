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
        var timeoutSeconds = configuration.GetValue<int>("Ollama:TimeoutSeconds", 120);

        // DocumentIntakeAgent — quality check on upload
        services.AddHttpClient<IDocumentIntakeAgent, DocumentIntakeAgent>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        // DocumentVerificationAgent — cross-check documents on submission (fire-and-forget)
        services.AddHttpClient<IDocumentVerificationAgent, DocumentVerificationAgent>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
        });

        return services;
    }
}

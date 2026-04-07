using MAEMS.Application.Interfaces;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using MAEMS.Infrastructure.Repositories;
using MAEMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MAEMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<postgresContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Repositories
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IMajorRepository, MajorRepository>();
        services.AddScoped<IProgramRepository, ProgramRepository>();
        services.AddScoped<ICampusRepository, CampusRepository>();
        services.AddScoped<IApplicantRepository, ApplicantRepository>();
        services.AddScoped<IAdmissionTypeRepository, AdmissionTypeRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ILlmChatLogRepository, LlmChatLogRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // Register LlmChatLogRepository as concrete type and legacy interface for backward compatibility with ChatBoxAgent and ChatBoxController
        services.AddScoped<LlmChatLogRepository>();
        services.AddScoped<ILlmChatLogRepositoryLegacy>(sp => sp.GetRequiredService<LlmChatLogRepository>());

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register JWT Service
        services.AddScoped<IJwtService, JwtService>();

        // Register Email Service
        services.AddScoped<IEmailService, EmailService>();

        // Register Token Service
        services.AddScoped<ITokenService, TokenService>();

        // Register Firebase Auth Service
        services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();

        // Register File Storage Service (Firebase implementation)
        services.AddSingleton<MAEMS.Application.Interfaces.IFileStorageService, FirebaseStorageService>();

        // Register Gemini Service
        services.AddHttpClient<IGeminiService, GeminiService>();

        // Payment expiration (in-process fire-and-forget)
        services.AddSingleton<IPaymentExpirationService, PaymentExpirationService>();

        return services;
    }
}

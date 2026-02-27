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

        return services;
    }
}

using MAEMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Concurrent;

namespace MAEMS.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateEmailVerificationToken(string email, string username)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "MAEMS_API";
        var audience = _configuration["JwtSettings:Audience"] ?? "MAEMS_Client";

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("purpose", "email_verification")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Token expires in 24 hours
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string? ValidateEmailVerificationToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"] ?? "MAEMS_API",
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"] ?? "MAEMS_Client",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            
            // Extract email from claims
            var emailClaim = principal.FindFirst(ClaimTypes.Email)?.Value;
            
            // Verify it's an email verification token
            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != "email_verification")
            {
                return null;
            }

            return emailClaim;
        }
        catch
        {
            return null;
        }
    }

    public Task<string> GenerateTokenAsync(string identifier, string purpose, TimeSpan expiration)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "MAEMS_API";
        var audience = _configuration["JwtSettings:Audience"] ?? "MAEMS_Client";

        var claims = new List<Claim>
        {
            new Claim("identifier", identifier),
            new Claim("purpose", purpose),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiration),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult(tokenString);
    }

    public Task<(bool IsValid, string Identifier)> ValidateTokenAsync(string token, string purpose)
    {
        try
        {
            // Check if token has been revoked
            if (_revokedTokens.ContainsKey(token))
            {
                return Task.FromResult((false, string.Empty));
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var key = Encoding.UTF8.GetBytes(secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"] ?? "MAEMS_API",
                ValidateAudience = true,
                ValidAudience = _configuration["JwtSettings:Audience"] ?? "MAEMS_Client",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            var purposeClaim = principal.FindFirst("purpose")?.Value;
            if (purposeClaim != purpose)
            {
                return Task.FromResult((false, string.Empty));
            }

            var identifierClaim = principal.FindFirst("identifier")?.Value ?? string.Empty;

            return Task.FromResult((true, identifierClaim));
        }
        catch
        {
            return Task.FromResult((false, string.Empty));
        }
    }

    public Task RevokeTokenAsync(string token)
    {
        // Add token to revoked list with expiration time
        _revokedTokens.TryAdd(token, DateTime.UtcNow.AddHours(2));

        // Clean up expired revoked tokens
        var expiredTokens = _revokedTokens.Where(kvp => kvp.Value < DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
        foreach (var expiredToken in expiredTokens)
        {
            _revokedTokens.TryRemove(expiredToken, out _);
        }

        return Task.CompletedTask;
    }
}

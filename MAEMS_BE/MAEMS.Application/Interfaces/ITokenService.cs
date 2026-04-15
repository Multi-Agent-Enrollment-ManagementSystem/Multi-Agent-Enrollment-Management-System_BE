namespace MAEMS.Application.Interfaces;

public interface ITokenService
{
    string GenerateEmailVerificationToken(string email, string username);
    string? ValidateEmailVerificationToken(string token);

    Task<string> GenerateTokenAsync(string identifier, string purpose, TimeSpan expiration);
    Task<(bool IsValid, string Identifier)> ValidateTokenAsync(string token, string purpose);
    Task RevokeTokenAsync(string token);
}

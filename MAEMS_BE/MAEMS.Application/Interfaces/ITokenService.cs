namespace MAEMS.Application.Interfaces;

public interface ITokenService
{
    string GenerateEmailVerificationToken(string email, string username);
    string? ValidateEmailVerificationToken(string token);
}

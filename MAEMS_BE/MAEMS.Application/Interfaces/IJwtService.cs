namespace MAEMS.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(int userId, string username, string email, string? roleName);
    DateTime GetTokenExpiration();
}

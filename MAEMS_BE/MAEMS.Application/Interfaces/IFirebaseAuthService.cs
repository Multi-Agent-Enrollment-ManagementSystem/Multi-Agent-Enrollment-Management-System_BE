namespace MAEMS.Application.Interfaces;

public interface IFirebaseAuthService
{
    Task<(bool IsValid, string? Email, string? Name)> ValidateGoogleTokenAsync(string idToken);
}

namespace MAEMS.Application.Interfaces;

public interface IOtpService
{
    string GenerateOtp();
    Task StoreOtpAsync(string email, string otpCode, int userId, TimeSpan expiration);
    Task<(bool IsValid, int UserId)> ValidateOtpAsync(string email, string otpCode);
    Task RevokeOtpAsync(string email);
}

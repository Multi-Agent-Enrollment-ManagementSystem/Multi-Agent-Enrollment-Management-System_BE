namespace MAEMS.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendVerificationEmailAsync(string toEmail, string username, string verificationToken);
    Task SendPasswordResetOtpEmailAsync(string toEmail, string username, string otpCode);
    Task SendPaymentReceivedEmailAsync(
        string toEmail,
        string? fullName,
        decimal amount,
        string? referenceCode,
        string? transactionId);

    Task SendApplicationStatusUpdatedEmailAsync(
        string toEmail,
        string? fullName,
        int applicationId);
}

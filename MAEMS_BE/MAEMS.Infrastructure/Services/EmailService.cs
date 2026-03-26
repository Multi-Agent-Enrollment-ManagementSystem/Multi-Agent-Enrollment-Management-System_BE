using MAEMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Mail;

namespace MAEMS.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _fromPassword;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
        _smtpHost = _configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("SMTP Host not configured");
        _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
        _fromEmail = _configuration["Email:FromEmail"] ?? throw new InvalidOperationException("From Email not configured");
        _fromPassword = _configuration["Email:FromPassword"] ?? throw new InvalidOperationException("From Password not configured");
        _fromName = _configuration["Email:FromName"] ?? "MAEMS";
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_fromEmail, _fromPassword)
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        mailMessage.To.Add(toEmail);

        await client.SendMailAsync(mailMessage);
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string verificationToken)
    {
        var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "https://localhost:7000";
        var verificationLink = $"{apiBaseUrl}/api/users/verify-email?token={verificationToken}";

        var subject = "Verify Your Email - MAEMS";
        var body = $@"
            <html>
            <body>
                <h2>Welcome to MAEMS, {username}!</h2>
                <p>Thank you for registering. Please verify your email address by clicking the link below:</p>
                <p><a href='{verificationLink}' style='background-color: #4CAF50; color: white; padding: 14px 20px; text-align: center; text-decoration: none; display: inline-block; border-radius: 4px;'>Verify Email</a></p>
                <p>Or copy and paste this link into your browser:</p>
                <p>{verificationLink}</p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not create an account, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>MAEMS Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendPaymentReceivedEmailAsync(
        string toEmail,
        string? fullName,
        decimal amount,
        string? referenceCode,
        string? transactionId)
    {
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var amountText = string.Format(culture, "{0:N0} đ", amount);

        var subject = "Xác nhận đã nhận được thanh toán lệ phí - MAEMS";
        var body = $@"
            <html>
            <body style='font-family: Arial, Helvetica, sans-serif; line-height: 1.6;'>
                <h2>Xin chào {WebUtility.HtmlEncode(displayName)},</h2>
                <p>Hệ thống MAEMS đã ghi nhận khoản thanh toán lệ phí xét tuyển của bạn.</p>

                <table style='border-collapse: collapse;'>
                    
                    <tr>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'><b>Số tiền</b></td>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'>{WebUtility.HtmlEncode(amountText)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'><b>Mã tham chiếu</b></td>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'>{WebUtility.HtmlEncode(referenceCode ?? string.Empty)}</td>
                    </tr>
                    <tr>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'><b>Mã giao dịch</b></td>
                        <td style='padding: 6px 12px; border: 1px solid #ddd;'>{WebUtility.HtmlEncode(transactionId ?? string.Empty)}</td>
                    </tr>
                </table>

                <p style='margin-top: 16px;'>Bạn có thể đăng nhập hệ thống và tiếp tục nộp (submit) hồ sơ.</p>
                <p>Nếu bạn không thực hiện giao dịch này hoặc cần hỗ trợ, vui lòng liên hệ bộ phận hỗ trợ.</p>

                <br/>
                <p>Trân trọng,<br/>MAEMS Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendApplicationStatusUpdatedEmailAsync(
        string toEmail,
        string? fullName,
        int applicationId)
    {
        var displayName = string.IsNullOrWhiteSpace(fullName) ? "bạn" : fullName;

        var subject = "Đơn đăng ký của bạn đã được cập nhật - MAEMS";
        var body = $@"
            <html>
            <body style='font-family: Arial, Helvetica, sans-serif; line-height: 1.6;'>
                <h2>Xin chào {WebUtility.HtmlEncode(displayName)},</h2>
                <p>Đơn đăng ký (ID: <b>{applicationId}</b>) của bạn đã có cập nhật.</p>
                <p>Vui lòng vào trang web để biết chi tiết.</p>
                <hr/>
                <br/>
                <p>Trân trọng,<br/>MAEMS Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }
}

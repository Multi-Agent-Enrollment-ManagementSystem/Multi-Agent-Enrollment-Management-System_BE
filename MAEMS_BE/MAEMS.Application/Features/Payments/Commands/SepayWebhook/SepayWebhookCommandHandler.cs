using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Application.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Payments.Commands.SepayWebhook;

public sealed class SepayWebhookCommandHandler : IRequestHandler<SepayWebhookCommand, BaseResponse<object?>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public SepayWebhookCommandHandler(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<BaseResponse<object?>> Handle(SepayWebhookCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transactionId = ExtractTransactionId(request.Content ?? string.Empty);

            if (string.IsNullOrWhiteSpace(transactionId))
            {
                return BaseResponse<object?>.FailureResponse(
                    "Invalid webhook data",
                    new List<string> { "TransactionId not found in webhook content" });
            }

            var payment = await _unitOfWork.Payments.GetByTransactionIdAsync(transactionId);
            if (payment == null)
            {
                return BaseResponse<object?>.FailureResponse(
                    "Payment not found",
                    new List<string> { $"Payment with TransactionId '{transactionId}' does not exist" });
            }

            // Load applicant -> user id for notifications
            if (!payment.ApplicantId.HasValue)
            {
                return BaseResponse<object?>.FailureResponse(
                    "Invalid payment",
                    new List<string> { "Payment has no ApplicantId" });
            }

            var applicant = await _unitOfWork.Applicants.GetByIdAsync(payment.ApplicantId.Value);
            if (applicant == null || !applicant.UserId.HasValue)
            {
                return BaseResponse<object?>.FailureResponse(
                    "Applicant profile not found",
                    new List<string> { "Applicant user not found for this payment" });
            }

            var recipientUserId = applicant.UserId.Value;

            var amountMatches = payment.Amount.HasValue && request.TransferAmount == payment.Amount.Value;
            var statusIsPending = string.Equals(payment.PaymentStatus, "pending", StringComparison.OrdinalIgnoreCase);

            if (amountMatches && statusIsPending)
            {
                payment.PaymentStatus = "Paid";
                payment.PaidAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                payment.ReferenceCode = request.ReferenceCode;
                await _unitOfWork.Payments.UpdateAsync(payment);

                var message =
                    $"Hệ thống đã ghi nhận khoản thanh toán lệ phí xét tuyển cho hồ sơ {payment.PaymentId} của bạn. " +
                    "Bây giờ bạn có thể submit hồ sơ.";

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    RecipientUserId = recipientUserId,
                    NotificationType = "Thanh toán",
                    Message = message,
                    IsRead = false,
                    SentAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });

                // Send email confirmation
                var user = await _unitOfWork.Users.GetByIdAsync(recipientUserId);
                if (!string.IsNullOrWhiteSpace(user?.Email) && payment.Amount.HasValue)
                {
                    await _emailService.SendPaymentReceivedEmailAsync(
                        user.Email,
                        user.Username,
                        payment.Amount.Value,
                        request.ReferenceCode,
                        transactionId);
                }

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<object?>.SuccessResponse(null, "Webhook processed: payment marked as Paid");
            }
            else
            {
                payment.PaymentStatus = "Need_checking";
                await _unitOfWork.Payments.UpdateAsync(payment);
                var message =
                    $"Hệ thống đã ghi nhận khoản thanh toán lệ phí xét tuyển cho hồ sơ {payment.PaymentId} của bạn không hợp lệ. " +
                    "Vui lòng liên lạc với nhân viên để hỗ trợ.";

                await _unitOfWork.Notifications.AddAsync(new Notification
                {
                    RecipientUserId = recipientUserId,
                    NotificationType = "Thanh toán",
                    Message = message,
                    IsRead = false,
                    SentAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });
                // Send email confirmation
                var user = await _unitOfWork.Users.GetByIdAsync(recipientUserId);
                if (!string.IsNullOrWhiteSpace(user?.Email) && payment.Amount.HasValue)
                {
                    await _emailService.SendPaymentReceivedEmailAsync(
                        user.Email,
                        user.Username,
                        payment.Amount.Value,
                        request.ReferenceCode,
                        transactionId);
                }

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<object?>.SuccessResponse(null, "Webhook processed: invalid payment notification sent");
            }
        }
        catch (Exception ex)
        {
            return BaseResponse<object?>.FailureResponse(
                "Error processing Sepay webhook",
                new List<string> { ex.Message });
        }
    }

    private static string ExtractTransactionId(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Space-separated format
        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            if (part.StartsWith("NAP", StringComparison.OrdinalIgnoreCase) && part.Length > 3)
                return part;
        }

        // Hyphen-separated format
        var hyphenParts = content.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in hyphenParts)
        {
            if (part.StartsWith("NAP", StringComparison.OrdinalIgnoreCase) && part.Length > 3)
                return part;
        }

        return string.Empty;
    }
}

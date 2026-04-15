using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using System.Security.Cryptography;

namespace MAEMS.Application.Features.Users.Commands.RequestPasswordReset;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, BaseResponse<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;

    public RequestPasswordResetCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IOtpService otpService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _otpService = otpService;
    }

    public async Task<BaseResponse<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BaseResponse<string>.FailureResponse(
                    "Email is required",
                    new List<string> { "Please provide a valid email address" });
            }

            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

            // For security reasons, always return success even if email doesn't exist
            // This prevents email enumeration attacks
            if (user == null)
            {
                return BaseResponse<string>.SuccessResponse(
                    "If an account exists with this email, an OTP code has been sent",
                    "OTP code sent");
            }

            // Check if user is active
            if (user.IsActive == false)
            {
                return BaseResponse<string>.SuccessResponse(
                    "If an account exists with this email, an OTP code has been sent",
                    "OTP code sent");
            }

            // Generate 6-digit OTP code
            var otpCode = _otpService.GenerateOtp();

            // Store OTP (expires in 10 minutes)
            await _otpService.StoreOtpAsync(user.Email, otpCode, user.UserId, TimeSpan.FromMinutes(10));

            // Send OTP via email
            await _emailService.SendPasswordResetOtpEmailAsync(user.Email, user.Username, otpCode);

            return BaseResponse<string>.SuccessResponse(
                "If an account exists with this email, an OTP code has been sent",
                "OTP code sent");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };

            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<string>.FailureResponse(
                "Failed to process password reset request",
                errors);
        }
    }
}

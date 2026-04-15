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
    private readonly ITokenService _tokenService;

    public RequestPasswordResetCommandHandler(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _tokenService = tokenService;
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
                    "If an account exists with this email, a password reset link has been sent",
                    "Password reset email sent");
            }

            // Check if user is active
            if (user.IsActive == false)
            {
                return BaseResponse<string>.SuccessResponse(
                    "If an account exists with this email, a password reset link has been sent",
                    "Password reset email sent");
            }

            // Generate reset token (expires in 1 hour)
            var resetToken = await _tokenService.GenerateTokenAsync(
                user.UserId.ToString(),
                "PasswordReset",
                TimeSpan.FromHours(1));

            // Send password reset email
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);

            return BaseResponse<string>.SuccessResponse(
                "If an account exists with this email, a password reset link has been sent",
                "Password reset email sent");
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

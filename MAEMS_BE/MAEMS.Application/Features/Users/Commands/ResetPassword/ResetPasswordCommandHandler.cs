using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, BaseResponse<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOtpService _otpService;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IOtpService otpService)
    {
        _unitOfWork = unitOfWork;
        _otpService = otpService;
    }

    public async Task<BaseResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BaseResponse<string>.FailureResponse(
                    "Email is required",
                    new List<string> { "Please provide your email address" });
            }

            if (string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return BaseResponse<string>.FailureResponse(
                    "OTP code is required",
                    new List<string> { "Please provide the OTP code sent to your email" });
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BaseResponse<string>.FailureResponse(
                    "Password is required",
                    new List<string> { "Please provide a new password" });
            }

            if (request.NewPassword.Length < 6)
            {
                return BaseResponse<string>.FailureResponse(
                    "Password too short",
                    new List<string> { "Password must be at least 6 characters long" });
            }

            if (request.NewPassword != request.ConfirmPassword)
            {
                return BaseResponse<string>.FailureResponse(
                    "Passwords do not match",
                    new List<string> { "New password and confirm password must match" });
            }

            // Validate OTP and get user ID
            var (isValid, userId) = await _otpService.ValidateOtpAsync(request.Email, request.OtpCode);

            if (!isValid)
            {
                return BaseResponse<string>.FailureResponse(
                    "Invalid or expired OTP",
                    new List<string> { "The OTP code is invalid or has expired. Please request a new one." });
            }

            // Get user
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                return BaseResponse<string>.FailureResponse(
                    "User not found",
                    new List<string> { "The user associated with this OTP does not exist" });
            }

            // Verify email matches
            if (!user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BaseResponse<string>.FailureResponse(
                    "Invalid request",
                    new List<string> { "Email does not match the OTP" });
            }

            // Check if user is active
            if (user.IsActive == false)
            {
                return BaseResponse<string>.FailureResponse(
                    "Account is inactive",
                    new List<string> { "Your account has been deactivated. Please contact support." });
            }

            // Hash new password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update user password
            user.PasswordHash = hashedPassword;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Revoke the OTP so it can't be used again
            await _otpService.RevokeOtpAsync(request.Email);

            return BaseResponse<string>.SuccessResponse(
                "Password has been reset successfully. You can now login with your new password.",
                "Password reset successful");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };

            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<string>.FailureResponse(
                "Failed to reset password",
                errors);
        }
    }
}

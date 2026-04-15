using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, BaseResponse<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<BaseResponse<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BaseResponse<string>.FailureResponse(
                    "Invalid token",
                    new List<string> { "Reset token is required" });
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

            // Validate token and get user ID
            var validationResult = await _tokenService.ValidateTokenAsync(request.Token, "PasswordReset");

            if (!validationResult.IsValid)
            {
                return BaseResponse<string>.FailureResponse(
                    "Invalid or expired token",
                    new List<string> { "The password reset link is invalid or has expired. Please request a new one." });
            }

            // Parse user ID from token
            if (!int.TryParse(validationResult.Identifier, out int userId))
            {
                return BaseResponse<string>.FailureResponse(
                    "Invalid token",
                    new List<string> { "The reset token is invalid" });
            }

            // Get user
            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)
            {
                return BaseResponse<string>.FailureResponse(
                    "User not found",
                    new List<string> { "The user associated with this token does not exist" });
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

            // Revoke the token so it can't be used again
            await _tokenService.RevokeTokenAsync(request.Token);

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

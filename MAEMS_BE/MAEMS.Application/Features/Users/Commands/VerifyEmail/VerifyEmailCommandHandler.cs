using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MAEMS.Application.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Users.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;

    public VerifyEmailCommandHandler(IUnitOfWork unitOfWork, ITokenService tokenService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
    }

    public async Task<BaseResponse<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate and extract email from token
            var email = _tokenService.ValidateEmailVerificationToken(request.Token);
            
            if (string.IsNullOrEmpty(email))
            {
                return BaseResponse<bool>.FailureResponse(
                    "Invalid or expired verification token",
                    new List<string> { "The verification link is invalid or has expired" });
            }

            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            
            if (user == null)
            {
                return BaseResponse<bool>.FailureResponse(
                    "User not found",
                    new List<string> { "No user found with this email" });
            }

            // Check if already verified
            if (user.IsActive == true)
            {
                return BaseResponse<bool>.FailureResponse(
                    "Email already verified",
                    new List<string> { "This email has already been verified" });
            }

            // Activate the user
            user.IsActive = true;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<bool>.SuccessResponse(true, "Email verified successfully. You can now login.");
        }
        catch (Exception ex)
        {
            var errors = new List<string> { ex.Message };
            
            if (ex.InnerException != null)
            {
                errors.Add($"Inner Exception: {ex.InnerException.Message}");
            }

            return BaseResponse<bool>.FailureResponse(
                "Failed to verify email",
                errors);
        }
    }
}

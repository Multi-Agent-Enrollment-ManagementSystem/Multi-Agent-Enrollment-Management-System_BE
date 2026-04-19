using FluentValidation;

namespace MAEMS.Application.Features.Users.Commands.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required")
            .MinimumLength(20).WithMessage("Invalid token format");
    }
}

using FluentValidation;

namespace MAEMS.Application.Features.Users.Commands.GoogleLogin;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("Google ID Token is required")
            .MinimumLength(10).WithMessage("Invalid Google ID Token format");
    }
}

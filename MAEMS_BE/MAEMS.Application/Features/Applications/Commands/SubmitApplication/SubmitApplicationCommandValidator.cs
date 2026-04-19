using FluentValidation;

namespace MAEMS.Application.Features.Applications.Commands.SubmitApplication;

public class SubmitApplicationCommandValidator : AbstractValidator<SubmitApplicationCommand>
{
    public SubmitApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .GreaterThan(0).WithMessage("Application ID must be greater than 0");
    }
}

using FluentValidation;

namespace MAEMS.Application.Features.Applications.Commands.CreateApplication;

public class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicantId)
            .GreaterThan(0).WithMessage("Applicant ID must be greater than 0");

        RuleFor(x => x.ConfigId)
            .GreaterThan(0).WithMessage("Config ID must be greater than 0");
    }
}

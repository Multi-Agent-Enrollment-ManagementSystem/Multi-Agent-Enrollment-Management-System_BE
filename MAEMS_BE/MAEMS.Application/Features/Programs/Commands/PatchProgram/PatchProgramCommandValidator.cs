using FluentValidation;

namespace MAEMS.Application.Features.Programs.Commands.PatchProgram;

public class PatchProgramCommandValidator : AbstractValidator<PatchProgramCommand>
{
    public PatchProgramCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .GreaterThan(0).WithMessage("Program ID must be greater than 0");

        RuleFor(x => x.ProgramName)
            .Length(3, 200).When(x => !string.IsNullOrEmpty(x.ProgramName))
            .WithMessage("Name must be between 3 and 200 characters");

        RuleFor(x => x.Duration)
            .Matches(@"^\d+\s+(year|years|semester|semesters)$")
            .When(x => !string.IsNullOrEmpty(x.Duration))
            .WithMessage("Duration must be in format: '4 years', '8 semesters', etc.");

        RuleFor(x => x.Description)
            .Length(50, 5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must be between 50 and 5000 characters");

        RuleFor(x => x.CareerProspects)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.CareerProspects))
            .WithMessage("Career prospects must not exceed 5000 characters");


    }
}

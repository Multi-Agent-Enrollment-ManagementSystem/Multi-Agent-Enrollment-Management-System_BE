using FluentValidation;

namespace MAEMS.Application.Features.Programs.Commands.CreateProgram;

public class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.ProgramName)
            .NotEmpty().WithMessage("Program name is required")
            .Length(3, 200).WithMessage("Name must be between 3 and 200 characters");

        RuleFor(x => x.MajorId)
            .GreaterThan(0).When(x => x.MajorId.HasValue)
            .WithMessage("Major ID must be greater than 0 if provided");

        RuleFor(x => x.Duration)
            .Matches(@"^\d+\s+(năm|học kỳ|năm \(.*\))$")
            .When(x => !string.IsNullOrEmpty(x.Duration))
            .WithMessage("Duration must be in format: '4 năm', '8 học kỳ', '3 năm (9 học kỳ)', etc.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .Length(50, 5000).WithMessage("Description must be between 50 and 5000 characters");

        RuleFor(x => x.CareerProspects)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.CareerProspects))
            .WithMessage("Career prospects must not exceed 5000 characters");


    }
}

using FluentValidation;

namespace MAEMS.Application.Features.Majors.Commands.PatchMajor;

public class PatchMajorCommandValidator : AbstractValidator<PatchMajorCommand>
{
    public PatchMajorCommandValidator()
    {
        RuleFor(x => x.MajorId)
            .GreaterThan(0).WithMessage("Major ID must be greater than 0");

        RuleFor(x => x.MajorCode)
            .Length(2, 20).When(x => !string.IsNullOrEmpty(x.MajorCode))
            .WithMessage("Code must be between 2 and 20 characters")
            .Matches(@"^[A-Z0-9]+$").When(x => !string.IsNullOrEmpty(x.MajorCode))
            .WithMessage("Code must contain only uppercase letters and numbers");

        RuleFor(x => x.MajorName)
            .Length(3, 200).When(x => !string.IsNullOrEmpty(x.MajorName))
            .WithMessage("Name must be between 3 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");
    }
}

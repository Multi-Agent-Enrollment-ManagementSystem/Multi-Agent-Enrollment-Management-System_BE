using FluentValidation;

namespace MAEMS.Application.Features.Majors.Commands.CreateMajor;

public class CreateMajorCommandValidator : AbstractValidator<CreateMajorCommand>
{
    public CreateMajorCommandValidator()
    {
        RuleFor(x => x.MajorCode)
            .NotEmpty().WithMessage("Major code is required")
            .Length(2, 20).WithMessage("Code must be between 2 and 20 characters")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Code must contain only uppercase letters and numbers");

        RuleFor(x => x.MajorName)
            .NotEmpty().WithMessage("Major name is required")
            .Length(3, 200).WithMessage("Name must be between 3 and 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(5000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 5000 characters");
    }
}

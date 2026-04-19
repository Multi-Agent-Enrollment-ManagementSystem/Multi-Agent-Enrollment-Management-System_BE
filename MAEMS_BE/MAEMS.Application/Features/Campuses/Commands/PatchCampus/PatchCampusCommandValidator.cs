using FluentValidation;

namespace MAEMS.Application.Features.Campuses.Commands.PatchCampus;

public class PatchCampusCommandValidator : AbstractValidator<PatchCampusCommand>
{
    public PatchCampusCommandValidator()
    {
        RuleFor(x => x.CampusId)
            .GreaterThan(0).WithMessage("Campus ID must be greater than 0");

        RuleFor(x => x.Name)
            .Length(3, 200).When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must be between 3 and 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Invalid email format")
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Email must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid Vietnamese phone number format");

        RuleFor(x => x.Address)
            .Length(10, 500).When(x => !string.IsNullOrEmpty(x.Address))
            .WithMessage("Address must be between 10 and 500 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 2000 characters");
    }
}

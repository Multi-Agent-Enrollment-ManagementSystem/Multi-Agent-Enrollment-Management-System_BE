using FluentValidation;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.CreateEnrollmentYear;

public class CreateEnrollmentYearCommandValidator : AbstractValidator<CreateEnrollmentYearCommand>
{
    private readonly string[] _validStatuses = { "upcoming", "ongoing", "completed", "closed" };

    public CreateEnrollmentYearCommandValidator()
    {
        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("Year is required")
            .Matches(@"^\d{4}(-\d{4})?$")
            .WithMessage("Year must be in format YYYY or YYYY-YYYY (e.g., 2024 or 2024-2025)");

        RuleFor(x => x.RegistrationStartDate)
            .NotNull().WithMessage("Registration start date is required");

        RuleFor(x => x.RegistrationEndDate)
            .NotNull().WithMessage("Registration end date is required")
            .GreaterThan(x => x.RegistrationStartDate)
            .When(x => x.RegistrationStartDate.HasValue && x.RegistrationEndDate.HasValue)
            .WithMessage("Registration end date must be after start date");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => _validStatuses.Contains(s.ToLower()))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");
    }
}

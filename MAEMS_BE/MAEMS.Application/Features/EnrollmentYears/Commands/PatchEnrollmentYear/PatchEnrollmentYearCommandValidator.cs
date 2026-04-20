using FluentValidation;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.PatchEnrollmentYear;

public class PatchEnrollmentYearCommandValidator : AbstractValidator<PatchEnrollmentYearCommand>
{
    private readonly string[] _validStatuses = { "upcoming", "active", "completed", "closed" };

    public PatchEnrollmentYearCommandValidator()
    {
        RuleFor(x => x.EnrollmentYearId)
            .GreaterThan(0).WithMessage("Enrollment year ID must be greater than 0");

        RuleFor(x => x.Year)
            .Matches(@"^\d{4}(-\d{4})?$")
            .When(x => !string.IsNullOrEmpty(x.Year))
            .WithMessage("Year must be in format YYYY or YYYY-YYYY (e.g., 2024 or 2024-2025)");

        RuleFor(x => x.RegistrationEndDate)
            .GreaterThan(x => x.RegistrationStartDate)
            .When(x => x.RegistrationStartDate.HasValue && x.RegistrationEndDate.HasValue)
            .WithMessage("Registration end date must be after start date");

        RuleFor(x => x.Status)
            .Must(s => _validStatuses.Contains(s?.ToLower()))
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");
    }
}

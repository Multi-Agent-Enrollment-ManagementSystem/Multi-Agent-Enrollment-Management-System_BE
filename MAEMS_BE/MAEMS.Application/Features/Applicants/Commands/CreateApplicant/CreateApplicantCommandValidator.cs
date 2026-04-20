using FluentValidation;

namespace MAEMS.Application.Features.Applicants.Commands.CreateApplicant;

public class CreateApplicantCommandValidator : AbstractValidator<CreateApplicantCommand>
{
    public CreateApplicantCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MinimumLength(3).WithMessage("Full name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters")
            .Matches(@"^[\p{L}\s]+$").WithMessage("Full name can only contain letters and spaces");

        RuleFor(x => x.DateOfBirth)
            .Must(BeAValidAge).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Age must be between 15 and 30 years old")
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past");

        RuleFor(x => x.Gender)
            .Must(x => x == null || new[] { "Nam", "Nữ", "Khác" }.Contains(x))
            .WithMessage("Gender must be Nam, Nữ, or Khác");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("Invalid email format");

        RuleFor(x => x.ContactPhone)
            .Matches(@"^(\+84|0)[0-9]{9,10}$").When(x => !string.IsNullOrEmpty(x.ContactPhone))
            .WithMessage("Invalid Vietnamese phone number format");

        RuleFor(x => x.GraduationYear)
            .LessThanOrEqualTo(DateTime.Now.Year).When(x => x.GraduationYear.HasValue)
            .WithMessage("Graduation year cannot be in the future")
            .GreaterThan(1950).When(x => x.GraduationYear.HasValue)
            .WithMessage("Graduation year must be after 1950");

        RuleFor(x => x.IdIssueNumber)
            .Matches(@"^[0-9]{9,12}$").When(x => !string.IsNullOrEmpty(x.IdIssueNumber))
            .WithMessage("ID number must be 9-12 digits");

        RuleFor(x => x.IdIssueDate)
            .LessThan(DateOnly.FromDateTime(DateTime.Now)).When(x => x.IdIssueDate.HasValue)
            .WithMessage("ID issue date must be in the past");
    }

    private bool BeAValidAge(DateOnly? dateOfBirth)
    {
        if (!dateOfBirth.HasValue) return true;

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value.ToDateTime(TimeOnly.MinValue) > today.AddYears(-age))
            age--;

        return age >= 15 && age <= 30;
    }
}

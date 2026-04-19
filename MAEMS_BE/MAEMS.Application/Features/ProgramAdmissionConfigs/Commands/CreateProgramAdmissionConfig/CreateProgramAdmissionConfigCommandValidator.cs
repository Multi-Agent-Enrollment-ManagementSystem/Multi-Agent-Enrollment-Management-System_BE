using FluentValidation;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.CreateProgramAdmissionConfig;

public class CreateProgramAdmissionConfigCommandValidator : AbstractValidator<CreateProgramAdmissionConfigCommand>
{
    public CreateProgramAdmissionConfigCommandValidator()
    {
        RuleFor(x => x.ProgramId)
            .NotNull().WithMessage("Program ID is required")
            .GreaterThan(0).WithMessage("Program ID must be greater than 0");

        RuleFor(x => x.CampusId)
            .NotNull().WithMessage("Campus ID is required")
            .GreaterThan(0).WithMessage("Campus ID must be greater than 0");

        RuleFor(x => x.AdmissionTypeId)
            .NotNull().WithMessage("Admission Type ID is required")
            .GreaterThan(0).WithMessage("Admission Type ID must be greater than 0");

        RuleFor(x => x.Quota)
            .NotNull().WithMessage("Quota is required")
            .GreaterThan(0).WithMessage("Quota must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Quota cannot exceed 10,000 students");
    }
}

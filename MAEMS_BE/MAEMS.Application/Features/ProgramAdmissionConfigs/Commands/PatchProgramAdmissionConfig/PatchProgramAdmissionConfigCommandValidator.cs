using FluentValidation;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.PatchProgramAdmissionConfig;

public class PatchProgramAdmissionConfigCommandValidator : AbstractValidator<PatchProgramAdmissionConfigCommand>
{
    public PatchProgramAdmissionConfigCommandValidator()
    {
        RuleFor(x => x.ConfigId)
            .GreaterThan(0).WithMessage("Config ID must be greater than 0");

        RuleFor(x => x.ProgramId)
            .GreaterThan(0).When(x => x.ProgramId.HasValue)
            .WithMessage("Program ID must be greater than 0 if provided");

        RuleFor(x => x.CampusId)
            .GreaterThan(0).When(x => x.CampusId.HasValue)
            .WithMessage("Campus ID must be greater than 0 if provided");

        RuleFor(x => x.AdmissionTypeId)
            .GreaterThan(0).When(x => x.AdmissionTypeId.HasValue)
            .WithMessage("Admission Type ID must be greater than 0 if provided");

        RuleFor(x => x.Quota)
            .GreaterThan(0).When(x => x.Quota.HasValue)
            .WithMessage("Quota must be greater than 0 if provided")
            .LessThanOrEqualTo(10000).When(x => x.Quota.HasValue)
            .WithMessage("Quota cannot exceed 10,000 students");
    }
}

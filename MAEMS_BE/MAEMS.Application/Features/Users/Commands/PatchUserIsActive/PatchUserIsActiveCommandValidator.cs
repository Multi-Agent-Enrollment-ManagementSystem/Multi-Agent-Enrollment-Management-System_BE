using FluentValidation;

namespace MAEMS.Application.Features.Users.Commands.PatchUserIsActive;

public class PatchUserIsActiveCommandValidator : AbstractValidator<PatchUserIsActiveCommand>
{
    public PatchUserIsActiveCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("IsActive status is required");

        RuleFor(x => x.RoleId)
            .GreaterThan(0).When(x => x.RoleId.HasValue)
            .WithMessage("Role ID must be greater than 0 if provided");
    }
}

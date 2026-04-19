using FluentValidation;

namespace MAEMS.Application.Features.Applications.Commands.PatchApplication;

public class PatchApplicationCommandValidator : AbstractValidator<PatchApplicationCommand>
{
    private readonly string[] _validStatuses =
    {
        "draft", "submitted", "under_review", "document_required",
        "approved", "rejected", "enrolled", "withdrawn"
    };

    public PatchApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .GreaterThan(0).WithMessage("Application ID must be greater than 0");

        RuleFor(x => x.Status)
            .Must(status => status == null || _validStatuses.Contains(status.ToLower()))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");
    }
}

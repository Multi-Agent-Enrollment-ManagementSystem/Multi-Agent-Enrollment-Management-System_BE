using FluentValidation;

namespace MAEMS.Application.Features.Applications.Commands.RequestAdditionalDocs;

public class RequestAdditionalDocsCommandValidator : AbstractValidator<RequestAdditionalDocsCommand>
{
    public RequestAdditionalDocsCommandValidator()
    {
        RuleFor(x => x.DocsNeed)
            .NotEmpty().WithMessage("Document requirements description is required")
            .MinimumLength(5).WithMessage("Description must be at least 5 characters")
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");
    }
}

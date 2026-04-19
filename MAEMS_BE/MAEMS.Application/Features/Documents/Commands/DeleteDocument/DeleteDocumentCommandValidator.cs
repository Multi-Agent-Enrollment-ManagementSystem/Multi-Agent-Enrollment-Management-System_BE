using FluentValidation;

namespace MAEMS.Application.Features.Documents.Commands.DeleteDocument;

public class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .GreaterThan(0).WithMessage("Document ID must be greater than 0");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
}

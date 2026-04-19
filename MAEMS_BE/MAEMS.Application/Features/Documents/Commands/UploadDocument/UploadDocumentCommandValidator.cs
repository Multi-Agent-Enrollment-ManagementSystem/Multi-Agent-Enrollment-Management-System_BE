using FluentValidation;

namespace MAEMS.Application.Features.Documents.Commands.UploadDocument;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .GreaterThan(0).WithMessage("Application ID must be greater than 0");

        RuleFor(x => x.File)
            .NotNull().WithMessage("File is required")
            .Must(file => file != null && file.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(file => file == null || file.Length <= _maxFileSize)
            .WithMessage($"File size must not exceed {_maxFileSize / (1024 * 1024)}MB")
            .Must(file => file == null || _allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            .WithMessage($"File type not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
    }
}

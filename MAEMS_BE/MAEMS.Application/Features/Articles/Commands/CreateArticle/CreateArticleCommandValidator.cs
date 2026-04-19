using FluentValidation;

namespace MAEMS.Application.Features.Articles.Commands.CreateArticle;

public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    private readonly string[] _validStatuses = { "draft", "published", "archived" };

    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .Length(10, 500).WithMessage("Title must be between 10 and 500 characters");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .Length(50, 50000).WithMessage("Content must be between 50 and 50,000 characters");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(s => _validStatuses.Contains(s?.ToLower()))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");

        RuleFor(x => x.Thumbnail)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Thumbnail))
            .WithMessage("Thumbnail URL must be a valid URL");

        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("Author ID must be greater than 0");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

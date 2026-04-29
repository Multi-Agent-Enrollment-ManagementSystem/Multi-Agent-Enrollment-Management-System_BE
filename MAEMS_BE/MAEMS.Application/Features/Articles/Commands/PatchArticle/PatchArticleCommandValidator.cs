using FluentValidation;

namespace MAEMS.Application.Features.Articles.Commands.PatchArticle;

public class PatchArticleCommandValidator : AbstractValidator<PatchArticleCommand>
{
    private readonly string[] _validStatuses = { "draft", "publish", "archived" };

    public PatchArticleCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .GreaterThan(0).WithMessage("Article ID must be greater than 0");

        RuleFor(x => x.Title)
            .Length(10, 500).When(x => !string.IsNullOrEmpty(x.Title))
            .WithMessage("Title must be between 10 and 500 characters");

        RuleFor(x => x.Content)
            .Length(50, 50000).When(x => !string.IsNullOrEmpty(x.Content))
            .WithMessage("Content must be between 50 and 50,000 characters");

        RuleFor(x => x.Status)
            .Must(s => _validStatuses.Contains(s?.ToLower()))
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");

        RuleFor(x => x.Thumbnail)
            .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Thumbnail))
            .WithMessage("Thumbnail URL must be a valid URL");
    }

    private bool BeValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}

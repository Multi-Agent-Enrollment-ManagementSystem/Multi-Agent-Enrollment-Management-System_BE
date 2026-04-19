using FluentValidation;

namespace MAEMS.Application.Features.Chat.Commands.AskChatBox;

public class AskChatBoxCommandValidator : AbstractValidator<AskChatBoxCommand>
{
    public AskChatBoxCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");

        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question is required")
            .MinimumLength(3).WithMessage("Question must be at least 3 characters")
            .MaximumLength(1000).WithMessage("Question must not exceed 1000 characters");
    }
}

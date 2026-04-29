using FluentValidation;

namespace MAEMS.Application.Features.RegisterEvents.Commands.CreateRegisterEvent;

public class CreateRegisterEventCommandValidator : AbstractValidator<CreateRegisterEventCommand>
{
    public CreateRegisterEventCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("ArticleId is required.")
            .GreaterThan(0).WithMessage("ArticleId must be greater than 0.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(150).WithMessage("FullName must not exceed 150 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not valid.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MaximumLength(10).WithMessage("Phone must not exceed 10 characters.");
    }
}
using FluentValidation;

namespace MAEMS.Application.Features.Payments.Commands.SepayWebhook;

public class SepayWebhookCommandValidator : AbstractValidator<SepayWebhookCommand>
{
    private readonly string[] _allowedGateways = { "TPBank", "Vietcombank", "VCB" };
    private const string _expectedAccountNumber = "10001993956";

    public SepayWebhookCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Transaction ID must be greater than 0");

        RuleFor(x => x.Gateway)
            .NotEmpty().WithMessage("Gateway is required")
            .Must(g => _allowedGateways.Contains(g ?? string.Empty))
            .WithMessage($"Gateway must be one of: {string.Join(", ", _allowedGateways)}");

        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("Transaction date is required")
            .Must(BeValidDateTimeFormat)
            .WithMessage("Invalid transaction date format");

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required")
            .Equal(_expectedAccountNumber)
            .WithMessage("Invalid account number - possible fraud attempt");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Transaction content is required")
            .MinimumLength(5).WithMessage("Content too short")
            .Matches(@"NAP\d+")
            .WithMessage("Content must contain valid transaction ID format (NAP...)");

        RuleFor(x => x.TransferAmount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0")
            .LessThanOrEqualTo(100_000_000)
            .WithMessage("Amount exceeds maximum allowed (100,000,000 VND)");

        RuleFor(x => x.ReferenceCode)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ReferenceCode))
            .WithMessage("Reference code must not exceed 100 characters");
    }

    private bool BeValidDateTimeFormat(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return false;
        return DateTime.TryParse(dateString, out _);
    }
}

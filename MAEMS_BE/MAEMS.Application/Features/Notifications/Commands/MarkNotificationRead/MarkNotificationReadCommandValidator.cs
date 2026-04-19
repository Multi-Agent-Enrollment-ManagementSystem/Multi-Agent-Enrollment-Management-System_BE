using FluentValidation;

namespace MAEMS.Application.Features.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .GreaterThan(0).WithMessage("Notification ID must be greater than 0");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0");
    }
}

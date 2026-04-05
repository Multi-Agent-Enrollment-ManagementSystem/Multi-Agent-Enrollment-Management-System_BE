using MAEMS.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MAEMS.Infrastructure.Services;

public sealed class PaymentExpirationService : IPaymentExpirationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PaymentExpirationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task ScheduleExpirePendingPaymentAsync(
        string transactionId,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        _ = RunExpirationAsync(transactionId, delay);
        return Task.CompletedTask;
    }

    private async Task RunExpirationAsync(string transactionId, TimeSpan delay)
    {
        await Task.Delay(delay, CancellationToken.None);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<MAEMS.Infrastructure.Models.postgresContext>();

        var payment = await db.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId, CancellationToken.None);

        if (payment is null)
            return;

        if (string.Equals(payment.PaymentStatus, "pending", StringComparison.OrdinalIgnoreCase))
        {
            payment.PaymentStatus = "outdated";
            db.Payments.Update(payment);
            await db.SaveChangesAsync(CancellationToken.None);
        }
    }
}
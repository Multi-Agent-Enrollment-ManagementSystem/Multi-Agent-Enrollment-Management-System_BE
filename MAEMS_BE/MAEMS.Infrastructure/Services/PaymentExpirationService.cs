using MAEMS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MAEMS.Infrastructure.Services;

/// <summary>
/// Simple in-process scheduler to mark pending payments as "outdated" after a TTL.
/// Note: this does not survive app restarts.
/// </summary>
public sealed class PaymentExpirationService : IPaymentExpirationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentExpirationService> _logger;

    public PaymentExpirationService(IServiceScopeFactory scopeFactory, ILogger<PaymentExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task ScheduleExpirePendingPaymentAsync(int paymentId, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        // Fire-and-forget background task.
        _ = Task.Run(async () =>
        {
            try
            {
                
                await Task.Delay(delay, CancellationToken.None);

                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<MAEMS.Infrastructure.Models.postgresContext>();
                var infra = await db.Payments.FindAsync([paymentId], cancellationToken);
                if (infra == null)
                    return;

                if (string.Equals(infra.PaymentStatus, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    infra.PaymentStatus = "outdated";
                    db.Payments.Update(infra);
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to expire pending payment {PaymentId}", paymentId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}

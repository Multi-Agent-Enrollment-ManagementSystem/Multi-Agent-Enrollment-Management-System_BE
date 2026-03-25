using MAEMS.Application.Interfaces;
using MAEMS.Domain.Interfaces;
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
                await Task.Delay(delay, cancellationToken);

                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // PaymentRepository exposes GetByTransactionIdAsync only; so read by applicationId is not efficient here.
                // We rely on PaymentId for update via repository UpdateAsync (requires entity with PaymentId).
                // Since IPaymentRepository doesn't have GetByIdAsync, we update by querying DbContext indirectly
                // through existing repository methods isn't possible. We'll use Payments.GetByApplicationIdAsync
                // via ApplicationId isn't available here.
                // => Use direct DbContext in Infrastructure scope.
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

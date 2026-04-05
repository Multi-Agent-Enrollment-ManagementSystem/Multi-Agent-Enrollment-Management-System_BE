namespace MAEMS.Application.Interfaces;

/// <summary>
/// Schedules a background check to expire (outdate) a pending payment after a TTL.
/// </summary>
public interface IPaymentExpirationService
{
    /// <summary>
    /// Fire-and-forget: after <paramref name="delay"/>, if the payment is still pending then mark it as "outdated".
    /// </summary>
    Task ScheduleExpirePendingPaymentAsync(string transactionId, TimeSpan delay, CancellationToken cancellationToken = default);
}

using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetByApplicationIdAsync(int applicationId);
    Task<IEnumerable<Payment>> GetByApplicantIdAsync(int applicantId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);

    Task<(IReadOnlyList<Payment> Items, int TotalCount)> GetPaymentsPagedAsync(
        int? applicationId,
        int? applicantId,
        string? status,
        string? method,
        string? transactionId,
        DateTime? paidFrom,
        DateTime? paidTo,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // SQL-level aggregates for reporting
    Task<int> CountDistinctPaidApplicantsAsync(CancellationToken cancellationToken = default);
    Task<int> CountDistinctPaidApplicationsAsync(CancellationToken cancellationToken = default);
    Task<int> CountNeedCheckingPaymentsAsync(CancellationToken cancellationToken = default);

    // Total revenue (sum Amount) for payments with status = Paid, grouped by quarter of a given year.
    // Returns items like: (Quarter: 1..4, TotalAmount).
    Task<IReadOnlyList<(int Quarter, decimal TotalAmount)>> GetPaidRevenueByQuarterAsync(
        int year,
        CancellationToken cancellationToken = default);

    Task<Payment> AddAsync(Payment entity);
    Task UpdateAsync(Payment entity);
}

using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainPayment = MAEMS.Domain.Entities.Payment;
using InfraPayment = MAEMS.Infrastructure.Models.Payment;

namespace MAEMS.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly postgresContext _context;

    public PaymentRepository(postgresContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DomainPayment>> GetByApplicationIdAsync(int applicationId)
    {
        var infraPayments = await _context.Payments
            .Where(p => p.ApplicationId == applicationId)
            .ToListAsync();

        return infraPayments.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainPayment>> GetByApplicantIdAsync(int applicantId)
    {
        var infraPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.ApplicantId == applicantId)
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.PaymentId)
            .ToListAsync();

        return infraPayments.Select(MapToDomain);
    }

    public async Task<DomainPayment?> GetByTransactionIdAsync(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return null;

        var infra = await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<(IReadOnlyList<DomainPayment> Items, int TotalCount)> GetPaymentsPagedAsync(
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
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Payments
            .AsNoTracking()
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(p => p.ApplicationId == applicationId.Value);

        if (applicantId.HasValue)
            query = query.Where(p => p.ApplicantId == applicantId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.PaymentStatus != null && EF.Functions.ILike(p.PaymentStatus, $"%{status}%"));

        if (!string.IsNullOrWhiteSpace(method))
            query = query.Where(p => p.PaymentMethod != null && EF.Functions.ILike(p.PaymentMethod, $"%{method}%"));

        if (!string.IsNullOrWhiteSpace(transactionId))
            query = query.Where(p => p.TransactionId != null && EF.Functions.ILike(p.TransactionId, $"%{transactionId}%"));

        if (paidFrom.HasValue)
            query = query.Where(p => p.PaidAt != null && p.PaidAt >= paidFrom.Value);

        if (paidTo.HasValue)
            query = query.Where(p => p.PaidAt != null && p.PaidAt <= paidTo.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "paymentId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "paymentid" => sortDesc ? query.OrderByDescending(x => x.PaymentId) : query.OrderBy(x => x.PaymentId),
            "applicationid" => sortDesc ? query.OrderByDescending(x => x.ApplicationId) : query.OrderBy(x => x.ApplicationId),
            "applicantid" => sortDesc ? query.OrderByDescending(x => x.ApplicantId) : query.OrderBy(x => x.ApplicantId),
            "amount" => sortDesc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "paymentmethod" => sortDesc ? query.OrderByDescending(x => x.PaymentMethod) : query.OrderBy(x => x.PaymentMethod),
            "transactionid" => sortDesc ? query.OrderByDescending(x => x.TransactionId) : query.OrderBy(x => x.TransactionId),
            "paymentstatus" => sortDesc ? query.OrderByDescending(x => x.PaymentStatus) : query.OrderBy(x => x.PaymentStatus),
            "paidat" => sortDesc ? query.OrderByDescending(x => x.PaidAt) : query.OrderBy(x => x.PaidAt),
            _ => sortDesc ? query.OrderByDescending(x => x.PaymentId) : query.OrderBy(x => x.PaymentId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    public async Task<DomainPayment> AddAsync(DomainPayment entity)
    {
        var infra = new InfraPayment
        {
            ApplicationId = entity.ApplicationId,
            ApplicantId = entity.ApplicantId,
            Amount = entity.Amount,
            PaymentMethod = entity.PaymentMethod ?? string.Empty,
            TransactionId = entity.TransactionId ?? string.Empty,
            PaymentStatus = entity.PaymentStatus ?? string.Empty,
            PaidAt = entity.PaidAt
        };

        await _context.Payments.AddAsync(infra);
        entity.PaymentId = infra.PaymentId;
        return entity;
    }

    public async Task UpdateAsync(DomainPayment entity)
    {
        var infra = await _context.Payments.FindAsync(entity.PaymentId);
        if (infra != null)
        {
            infra.ApplicationId = entity.ApplicationId;
            infra.ApplicantId = entity.ApplicantId;
            infra.Amount = entity.Amount;
            infra.PaymentMethod = entity.PaymentMethod;
            infra.TransactionId = entity.TransactionId;
            infra.PaymentStatus = entity.PaymentStatus;
            infra.PaidAt = entity.PaidAt;

            _context.Payments.Update(infra);
        }

        await Task.CompletedTask;
    }

    private static DomainPayment MapToDomain(InfraPayment infra) => new()
    {
        PaymentId = infra.PaymentId,
        ApplicationId = infra.ApplicationId,
        ApplicantId = infra.ApplicantId,
        Amount = infra.Amount,
        PaymentMethod = infra.PaymentMethod,
        TransactionId = infra.TransactionId,
        PaymentStatus = infra.PaymentStatus,
        PaidAt = infra.PaidAt
    };
}

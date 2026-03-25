using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Payment;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Payments.Queries.GetAllPayments;

public record GetAllPaymentsQuery(
    string? Status = null,
    string? TransactionId = null,
    DateTime? PaidFrom = null,
    DateTime? PaidTo = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<PaymentDto>>>;

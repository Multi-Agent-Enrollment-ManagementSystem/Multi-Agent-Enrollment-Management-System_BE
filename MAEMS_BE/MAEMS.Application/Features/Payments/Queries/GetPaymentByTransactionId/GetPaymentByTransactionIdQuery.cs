using MAEMS.Application.DTOs.Payment;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Payments.Queries.GetPaymentByTransactionId;

public record GetPaymentByTransactionIdQuery(string TransactionId)
    : IRequest<BaseResponse<PaymentDto>>;

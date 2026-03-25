using AutoMapper;
using MAEMS.Application.DTOs.Payment;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Payments.Queries.GetPaymentByTransactionId;

public class GetPaymentByTransactionIdQueryHandler
    : IRequestHandler<GetPaymentByTransactionIdQuery, BaseResponse<PaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaymentByTransactionIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PaymentDto>> Handle(GetPaymentByTransactionIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return BaseResponse<PaymentDto>.FailureResponse(
                    "Invalid transactionId",
                    new List<string> { "transactionId is required" });
            }

            var payment = await _unitOfWork.Payments.GetByTransactionIdAsync(request.TransactionId.Trim());

            if (payment == null)
            {
                return BaseResponse<PaymentDto>.FailureResponse(
                    "Payment not found",
                    new List<string> { $"No payment found with transactionId: {request.TransactionId}" });
            }

            var dto = _mapper.Map<PaymentDto>(payment);
            return BaseResponse<PaymentDto>.SuccessResponse(dto, "Payment retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PaymentDto>.FailureResponse(
                "Error retrieving payment",
                new List<string> { ex.Message });
        }
    }
}

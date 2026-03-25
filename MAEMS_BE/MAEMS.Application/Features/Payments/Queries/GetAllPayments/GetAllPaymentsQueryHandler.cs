using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Payment;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Payments.Queries.GetAllPayments;

public class GetAllPaymentsQueryHandler : IRequestHandler<GetAllPaymentsQuery, BaseResponse<PagedResponse<PaymentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllPaymentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<PaymentDto>>> Handle(GetAllPaymentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.Payments.GetPaymentsPagedAsync(
                applicationId: null,
                applicantId: null,
                status: request.Status,
                method: null,
                transactionId: request.TransactionId,
                paidFrom: request.PaidFrom,
                paidTo: request.PaidTo,
                sortBy: request.SortBy,
                sortDesc: request.SortDesc,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken);

            var dtos = items.Select(p => _mapper.Map<PaymentDto>(p)).ToList();

            var paged = new PagedResponse<PaymentDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<PaymentDto>>.SuccessResponse(paged, "Payments retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<PaymentDto>>.FailureResponse(
                "Error retrieving payments",
                new List<string> { ex.Message }
            );
        }
    }
}

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetPaidRevenueByQuarter;

public sealed class GetPaidRevenueByQuarterQueryHandler : IRequestHandler<GetPaidRevenueByQuarterQuery, BaseResponse<PaidRevenueByQuarterDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPaidRevenueByQuarterQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<PaidRevenueByQuarterDto>> Handle(GetPaidRevenueByQuarterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Year < 1)
                return BaseResponse<PaidRevenueByQuarterDto>.FailureResponse("Invalid year", new List<string> { "Year must be a valid positive integer" });

            var data = await _unitOfWork.Payments.GetPaidRevenueByQuarterAsync(request.Year, cancellationToken);
            var numNeedCheck = await _unitOfWork.Payments.CountNeedCheckingPaymentsAsync(cancellationToken);

            // Ensure all quarters exist in response (Q1..Q4)
            var dict = data.ToDictionary(x => x.Quarter, x => x.TotalAmount);
            var quarters = Enumerable.Range(1, 4)
                .Select(q => new PaidRevenueQuarterItemDto
                {
                    Quarter = q,
                    TotalAmount = dict.TryGetValue(q, out var total) ? total : 0m
                })
                .ToList();

            var dto = new PaidRevenueByQuarterDto
            {
                Year = request.Year,
                NumPaymentNeedCheck = numNeedCheck,
                Quarters = quarters
            };

            return BaseResponse<PaidRevenueByQuarterDto>.SuccessResponse(dto, "Paid revenue by quarter retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PaidRevenueByQuarterDto>.FailureResponse(
                "Error retrieving paid revenue by quarter",
                new List<string> { ex.Message });
        }
    }
}

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationStatusCounts;

public sealed class GetApplicationStatusCountsQueryHandler
    : IRequestHandler<GetApplicationStatusCountsQuery, BaseResponse<ApplicationStatusCountsDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetApplicationStatusCountsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<ApplicationStatusCountsDto>> Handle(
        GetApplicationStatusCountsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (numApproved, numRejected, numPending) =
                await _unitOfWork.Applications.CountApprovedRejectedPendingAsync(cancellationToken);

            var dto = new ApplicationStatusCountsDto
            {
                NumApproved = numApproved,
                NumRejected = numRejected,
                NumPending = numPending
            };

            return BaseResponse<ApplicationStatusCountsDto>.SuccessResponse(
                dto,
                "Application status counts retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicationStatusCountsDto>.FailureResponse(
                "Error retrieving application status counts",
                new List<string> { ex.Message });
        }
    }
}

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByAssignedOfficer;

public sealed class GetApplicationsCountByAssignedOfficerQueryHandler
    : IRequestHandler<GetApplicationsCountByAssignedOfficerQuery, BaseResponse<List<OfficerApplicationsCountDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetApplicationsCountByAssignedOfficerQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<OfficerApplicationsCountDto>>> Handle(
        GetApplicationsCountByAssignedOfficerQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _unitOfWork.Applications.CountByAssignedOfficerAsync(cancellationToken);

            var data = rows
                .Select(x => new OfficerApplicationsCountDto
                {
                    AssignedOfficerId = x.AssignedOfficerId,
                    AssignedOfficerName = x.AssignedOfficerName,
                    Count = x.Count
                })
                .ToList();

            return BaseResponse<List<OfficerApplicationsCountDto>>.SuccessResponse(
                data,
                "Applications count by assigned officer retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<List<OfficerApplicationsCountDto>>.FailureResponse(
                "Error retrieving applications count by assigned officer",
                new List<string> { ex.Message });
        }
    }
}

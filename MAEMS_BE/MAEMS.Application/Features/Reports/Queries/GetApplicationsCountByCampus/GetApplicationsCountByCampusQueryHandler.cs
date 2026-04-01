using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByCampus;

public sealed class GetApplicationsCountByCampusQueryHandler
    : IRequestHandler<GetApplicationsCountByCampusQuery, BaseResponse<List<CampusApplicationsCountDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetApplicationsCountByCampusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<CampusApplicationsCountDto>>> Handle(
        GetApplicationsCountByCampusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _unitOfWork.Applications.CountByCampusAsync(cancellationToken);

            var data = rows
                .Select(x => new CampusApplicationsCountDto
                {
                    CampusId = x.CampusId,
                    CampusName = x.CampusName,
                    Count = x.Count
                })
                .ToList();

            return BaseResponse<List<CampusApplicationsCountDto>>.SuccessResponse(
                data,
                "Applications count by campus retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<List<CampusApplicationsCountDto>>.FailureResponse(
                "Error retrieving applications count by campus",
                new List<string> { ex.Message });
        }
    }
}

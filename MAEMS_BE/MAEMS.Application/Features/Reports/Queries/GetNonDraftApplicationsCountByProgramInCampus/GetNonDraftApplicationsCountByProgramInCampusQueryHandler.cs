using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetNonDraftApplicationsCountByProgramInCampus;

public sealed class GetNonDraftApplicationsCountByProgramInCampusQueryHandler
    : IRequestHandler<GetNonDraftApplicationsCountByProgramInCampusQuery, BaseResponse<List<ProgramApplicationsCountDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNonDraftApplicationsCountByProgramInCampusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<ProgramApplicationsCountDto>>> Handle(
        GetNonDraftApplicationsCountByProgramInCampusQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.CampusId <= 0)
            {
                return BaseResponse<List<ProgramApplicationsCountDto>>.FailureResponse(
                    "Invalid campusId",
                    new List<string> { "campusId must be greater than 0" });
            }

            var rows = await _unitOfWork.Applications.CountNonDraftByProgramInCampusAsync(request.CampusId, cancellationToken);

            var data = rows
                .Select(x => new ProgramApplicationsCountDto
                {
                    ProgramId = x.ProgramId,
                    ProgramName = x.ProgramName,
                    Count = x.Count
                })
                .ToList();

            return BaseResponse<List<ProgramApplicationsCountDto>>.SuccessResponse(
                data,
                "Non-draft applications count by program in campus retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<List<ProgramApplicationsCountDto>>.FailureResponse(
                "Error retrieving non-draft applications count by program in campus",
                new List<string> { ex.Message });
        }
    }
}

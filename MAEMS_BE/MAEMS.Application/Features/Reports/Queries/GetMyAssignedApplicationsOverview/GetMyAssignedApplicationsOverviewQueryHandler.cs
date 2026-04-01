using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetMyAssignedApplicationsOverview;

public sealed class GetMyAssignedApplicationsOverviewQueryHandler
    : IRequestHandler<GetMyAssignedApplicationsOverviewQuery, BaseResponse<OfficerApplicationsOverviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyAssignedApplicationsOverviewQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<OfficerApplicationsOverviewDto>> Handle(
        GetMyAssignedApplicationsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.OfficerUserId <= 0)
            {
                return BaseResponse<OfficerApplicationsOverviewDto>.FailureResponse(
                    "Invalid officer user id",
                    new List<string> { "OfficerUserId must be greater than 0" });
            }

            // Load all applications assigned to this officer
            var apps = await _unitOfWork.Applications.GetByAssignedOfficerIdAsync(request.OfficerUserId);
            var appList = apps.ToList();

            var dtoApps = _mapper.Map<List<FullApplicationDto>>(appList);

            var total = appList.Count;
            var approved = appList.Count(a => string.Equals(a.Status, "approved", StringComparison.OrdinalIgnoreCase));
            var rejected = appList.Count(a => string.Equals(a.Status, "rejected", StringComparison.OrdinalIgnoreCase));

            var dto = new OfficerApplicationsOverviewDto
            {
                TotalCount = total,
                ApprovedCount = approved,
                RejectedCount = rejected,
                Applications = dtoApps
            };

            return BaseResponse<OfficerApplicationsOverviewDto>.SuccessResponse(dto, "Officer applications overview retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<OfficerApplicationsOverviewDto>.FailureResponse(
                "Error retrieving officer applications overview",
                new List<string> { ex.Message });
        }
    }
}

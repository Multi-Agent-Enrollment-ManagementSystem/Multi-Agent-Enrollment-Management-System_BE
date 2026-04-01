using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetMyAssignedApplicationsOverview;

public sealed record GetMyAssignedApplicationsOverviewQuery(int OfficerUserId)
    : IRequest<BaseResponse<OfficerApplicationsOverviewDto>>;

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByAssignedOfficer;

public sealed record GetApplicationsCountByAssignedOfficerQuery()
    : IRequest<BaseResponse<List<OfficerApplicationsCountDto>>>;

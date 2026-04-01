using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationsCountByCampus;

public sealed record GetApplicationsCountByCampusQuery()
    : IRequest<BaseResponse<List<CampusApplicationsCountDto>>>;

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetWeeklySubmittedApplicationsCount;

public sealed record GetWeeklySubmittedApplicationsCountQuery(DateTime? From, DateTime? To)
    : IRequest<BaseResponse<List<WeeklyApplicationsCountDto>>>;

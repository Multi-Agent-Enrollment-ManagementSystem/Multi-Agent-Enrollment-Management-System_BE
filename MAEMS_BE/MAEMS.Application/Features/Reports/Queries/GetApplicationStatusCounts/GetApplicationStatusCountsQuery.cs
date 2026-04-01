using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetApplicationStatusCounts;

public sealed record GetApplicationStatusCountsQuery()
    : IRequest<BaseResponse<ApplicationStatusCountsDto>>;

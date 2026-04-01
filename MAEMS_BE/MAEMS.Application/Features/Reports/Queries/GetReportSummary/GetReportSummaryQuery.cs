using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetReportSummary;

public sealed record GetReportSummaryQuery() : IRequest<BaseResponse<ReportSummaryDto>>;

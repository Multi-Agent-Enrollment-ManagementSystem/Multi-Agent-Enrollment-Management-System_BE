using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetPaidRevenueByQuarter;

public sealed record GetPaidRevenueByQuarterQuery(int Year) : IRequest<BaseResponse<PaidRevenueByQuarterDto>>;

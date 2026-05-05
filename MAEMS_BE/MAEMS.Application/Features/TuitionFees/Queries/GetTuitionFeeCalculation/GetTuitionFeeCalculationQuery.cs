using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;

namespace MAEMS.Application.Features.TuitionFees.Queries.GetTuitionFeeCalculation;

public record GetTuitionFeeCalculationQuery : IRequest<BaseResponse<TuitionFeeCalculationResponse>>
{
    public string MajorName { get; init; } = string.Empty;
    public string CampusName { get; init; } = string.Empty;
    public string Region { get; init; } = "OTHER";
    public int? EnrollmentYearId { get; init; }
}

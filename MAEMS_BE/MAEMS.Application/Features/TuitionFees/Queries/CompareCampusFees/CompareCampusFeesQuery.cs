using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;

namespace MAEMS.Application.Features.TuitionFees.Queries.CompareCampusFees;

public record CompareCampusFeesQuery : IRequest<BaseResponse<IEnumerable<CampusFeeComparisonResponse>>>
{
    public string MajorName { get; init; } = string.Empty;
    public string Region { get; init; } = "OTHER";
}

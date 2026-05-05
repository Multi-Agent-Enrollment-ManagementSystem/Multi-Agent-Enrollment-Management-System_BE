using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;

namespace MAEMS.Application.Features.TuitionFees.Queries.GetAllActiveTuitionFees;

public record GetAllActiveTuitionFeesQuery : IRequest<BaseResponse<IEnumerable<TuitionFeeDto>>>;

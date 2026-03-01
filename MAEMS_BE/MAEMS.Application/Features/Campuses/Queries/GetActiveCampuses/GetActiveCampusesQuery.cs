using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetActiveCampuses;

public record GetActiveCampusesQuery : IRequest<BaseResponse<IEnumerable<CampusDto>>>;

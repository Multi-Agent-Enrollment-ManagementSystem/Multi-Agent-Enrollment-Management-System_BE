using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetAllCampuses;

public record GetAllCampusesQuery : IRequest<BaseResponse<IEnumerable<CampusDto>>>;

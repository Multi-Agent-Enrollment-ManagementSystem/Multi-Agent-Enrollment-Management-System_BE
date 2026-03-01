using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetActiveCampusesBasic;

public record GetActiveCampusesBasicQuery : IRequest<BaseResponse<IEnumerable<CampusBasicDto>>>;

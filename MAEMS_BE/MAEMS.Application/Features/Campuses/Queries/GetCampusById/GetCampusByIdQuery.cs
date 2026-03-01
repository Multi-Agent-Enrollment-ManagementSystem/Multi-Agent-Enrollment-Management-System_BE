using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetCampusById;

public record GetCampusByIdQuery(int Id) : IRequest<BaseResponse<CampusDto>>;

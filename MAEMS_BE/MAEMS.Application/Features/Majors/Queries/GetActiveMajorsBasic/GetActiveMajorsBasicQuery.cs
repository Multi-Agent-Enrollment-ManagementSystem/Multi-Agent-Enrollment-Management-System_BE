using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetActiveMajorsBasic;

public record GetActiveMajorsBasicQuery : IRequest<BaseResponse<IEnumerable<MajorBasicDto>>>;

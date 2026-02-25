using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetActiveMajors;

public record GetActiveMajorsQuery : IRequest<BaseResponse<IEnumerable<MajorDto>>>;

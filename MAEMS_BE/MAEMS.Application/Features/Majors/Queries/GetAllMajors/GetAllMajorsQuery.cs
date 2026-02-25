using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetAllMajors;

public record GetAllMajorsQuery : IRequest<BaseResponse<IEnumerable<MajorDto>>>;

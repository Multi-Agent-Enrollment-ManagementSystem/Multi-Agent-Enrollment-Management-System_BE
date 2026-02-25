using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetMajorById;

public record GetMajorByIdQuery(int Id) : IRequest<BaseResponse<MajorDto>>;

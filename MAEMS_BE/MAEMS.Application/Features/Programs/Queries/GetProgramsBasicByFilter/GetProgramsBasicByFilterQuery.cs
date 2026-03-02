using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramsBasicByFilter;

public record GetProgramsBasicByFilterQuery(int? MajorId, string? SearchName) : IRequest<BaseResponse<IEnumerable<ProgramBasicDto>>>;

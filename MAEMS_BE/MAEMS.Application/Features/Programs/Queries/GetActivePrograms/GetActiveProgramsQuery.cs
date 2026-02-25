using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetActivePrograms;

public record GetActiveProgramsQuery : IRequest<BaseResponse<IEnumerable<ProgramDto>>>;

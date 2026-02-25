using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetAllPrograms;

public record GetAllProgramsQuery : IRequest<BaseResponse<IEnumerable<ProgramDto>>>;

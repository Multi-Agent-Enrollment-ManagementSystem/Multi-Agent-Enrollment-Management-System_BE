using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetActiveProgramsBasic;

public record GetActiveProgramsBasicQuery : IRequest<BaseResponse<IEnumerable<ProgramBasicDto>>>;

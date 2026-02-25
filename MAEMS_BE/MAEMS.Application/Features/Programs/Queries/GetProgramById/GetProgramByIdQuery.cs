using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramById;

public record GetProgramByIdQuery(int Id) : IRequest<BaseResponse<ProgramDto>>;

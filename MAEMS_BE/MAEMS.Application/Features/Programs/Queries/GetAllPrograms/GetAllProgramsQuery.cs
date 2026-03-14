using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetAllPrograms;

public record GetAllProgramsQuery(int? MajorId = null, int? EnrollmentYearId = null) : IRequest<BaseResponse<IEnumerable<ProgramDto>>>;

using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetAllPrograms;

public record GetAllProgramsQuery(
    int? MajorId = null,
    int? EnrollmentYearId = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<ProgramDto>>>;

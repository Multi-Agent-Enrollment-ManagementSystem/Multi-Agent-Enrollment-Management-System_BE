using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramsBasicByFilter;

public record GetProgramsBasicByFilterQuery(
    int? MajorId,
    string? SearchName,
    string? SortBy,
    bool SortDesc,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<ProgramBasicDto>>>;

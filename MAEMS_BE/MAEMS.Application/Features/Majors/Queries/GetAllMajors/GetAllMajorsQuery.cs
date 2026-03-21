using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetAllMajors;

public record GetAllMajorsQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<MajorDto>>>;

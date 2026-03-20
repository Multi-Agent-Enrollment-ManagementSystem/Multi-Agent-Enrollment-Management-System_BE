using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;

public record GetAllProgramAdmissionConfigsQuery(
    int? ProgramId = null,
    int? CampusId = null,
    int? AdmissionTypeId = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<ProgramAdmissionConfigDto>>>;

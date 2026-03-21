using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;

public record GetAllFullApplicationsQuery(
    int? ProgramId = null,
    int? CampusId = null,
    int? AdmissionTypeId = null,
    string? Status = null,
    bool? RequiresReview = null,
    int? AssignedOfficerId = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<FullApplicationDto>>>;
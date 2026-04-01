using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetNonDraftApplicationsCountByProgramInCampus;

public sealed record GetNonDraftApplicationsCountByProgramInCampusQuery(int CampusId)
    : IRequest<BaseResponse<List<ProgramApplicationsCountDto>>>;

using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigsByFilter;

public record GetProgramAdmissionConfigsByFilterQuery(
    int? ProgramId,
    int? CampusId,
    int? AdmissionTypeId
) : IRequest<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>;

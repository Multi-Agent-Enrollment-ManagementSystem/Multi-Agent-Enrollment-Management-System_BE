using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetActiveProgramAdmissionConfigs;

public record GetActiveProgramAdmissionConfigsQuery : IRequest<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>;

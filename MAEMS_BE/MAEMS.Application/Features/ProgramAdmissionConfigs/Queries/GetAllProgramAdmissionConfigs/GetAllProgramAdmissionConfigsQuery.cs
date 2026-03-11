using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;

public record GetAllProgramAdmissionConfigsQuery : IRequest<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>;

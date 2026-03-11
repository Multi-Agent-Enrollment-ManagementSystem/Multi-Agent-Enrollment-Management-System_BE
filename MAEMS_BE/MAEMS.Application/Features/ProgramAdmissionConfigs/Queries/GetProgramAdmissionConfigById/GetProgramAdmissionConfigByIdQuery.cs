using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigById;

public record GetProgramAdmissionConfigByIdQuery(int ConfigId)
    : IRequest<BaseResponse<ProgramAdmissionConfigDto>>;

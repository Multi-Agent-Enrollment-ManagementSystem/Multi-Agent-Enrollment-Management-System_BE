using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.CreateProgramAdmissionConfig;

public class CreateProgramAdmissionConfigCommand : IRequest<BaseResponse<ProgramAdmissionConfigDto>>
{
    public int? ProgramId { get; set; }
    public int? CampusId { get; set; }
    public int? AdmissionTypeId { get; set; }
    public int? Quota { get; set; }
    public bool? IsActive { get; set; } = true;
}

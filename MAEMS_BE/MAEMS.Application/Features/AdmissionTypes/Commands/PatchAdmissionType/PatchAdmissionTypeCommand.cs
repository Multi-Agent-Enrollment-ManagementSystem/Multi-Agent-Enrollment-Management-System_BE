using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Commands.PatchAdmissionType;

public class PatchAdmissionTypeCommand : IRequest<BaseResponse<AdmissionTypeDto>>
{
    public int AdmissionTypeId { get; set; }

    public string? AdmissionTypeName { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? Type { get; set; }
    public string? RequiredDocumentList { get; set; }
    public string? EligibilityRules { get; set; }
    public string? PriorityRules { get; set; }
    public bool? IsActive { get; set; }
}

using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Commands.CreateAdmissionType;

public class CreateAdmissionTypeCommand : IRequest<BaseResponse<AdmissionTypeDto>>
{
    public string AdmissionTypeName { get; set; } = string.Empty;
    public int? EnrollmentYearId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? RequiredDocumentList { get; set; }
    public string? EligibilityRules { get; set; }
    public string? PriorityRules { get; set; }
    public bool? IsActive { get; set; } = true;
}

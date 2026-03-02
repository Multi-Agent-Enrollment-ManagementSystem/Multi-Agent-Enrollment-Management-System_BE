namespace MAEMS.Application.DTOs.AdmissionType;

public class AdmissionTypeDto
{
    public int AdmissionTypeId { get; set; }
    public string AdmissionTypeName { get; set; } = string.Empty;
    public string? EnrollmentYear { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? RequiredDocumentList { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

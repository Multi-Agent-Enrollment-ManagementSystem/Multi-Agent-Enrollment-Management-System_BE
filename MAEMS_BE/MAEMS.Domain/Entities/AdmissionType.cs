namespace MAEMS.Domain.Entities;

public class AdmissionType
{
    public int AdmissionTypeId { get; set; }
    public string AdmissionTypeName { get; set; } = string.Empty;
    public int? EnrollmentYearId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? RequiredDocumentList { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    // Navigation property
    public string? EnrollmentYear { get; set; }
}

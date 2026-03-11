namespace MAEMS.Application.DTOs.ProgramAdmissionConfig;

public class ProgramAdmissionConfigDto
{
    public int ConfigId { get; set; }
    public int? ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public int? CampusId { get; set; }
    public string? CampusName { get; set; }
    public int? AdmissionTypeId { get; set; }
    public string? AdmissionTypeName { get; set; }
    public int? Quota { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

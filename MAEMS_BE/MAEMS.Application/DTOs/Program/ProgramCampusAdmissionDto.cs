namespace MAEMS.Application.DTOs.Program;

public class ProgramCampusAdmissionDto
{
    public int ConfigId { get; set; }
    public int? AdmissionTypeId { get; set; }
    public string? AdmissionTypeName { get; set; }
    public int? Quota { get; set; }
    public bool? IsActive { get; set; }
}

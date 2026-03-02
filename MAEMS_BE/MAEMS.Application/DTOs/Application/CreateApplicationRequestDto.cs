namespace MAEMS.Application.DTOs.Application;

public class CreateApplicationRequestDto
{
    public int ProgramId { get; set; }
    public int EnrollmentYearId { get; set; }
    public int CampusId { get; set; }
    public int AdmissionTypeId { get; set; }
    public string? Notes { get; set; }
}
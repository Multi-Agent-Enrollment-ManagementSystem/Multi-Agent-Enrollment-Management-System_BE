namespace MAEMS.Application.DTOs.Application;

public class UpdateApplicationRequestDto
{
    public int? ProgramId { get; set; }
    public int? EnrollmentYearId { get; set; }
    public int? CampusId { get; set; }
    public int? AdmissionTypeId { get; set; }
    public string? Status { get; set; }
    public int? AssignedOfficerId { get; set; }
    public string? Notes { get; set; }
    public bool? RequiresReview { get; set; }
}
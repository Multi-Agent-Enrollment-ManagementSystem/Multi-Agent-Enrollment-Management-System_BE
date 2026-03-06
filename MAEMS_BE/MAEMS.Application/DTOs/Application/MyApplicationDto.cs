namespace MAEMS.Application.DTOs.Application;

public class MyApplicationDto
{
    public int ApplicationId { get; set; }
    public string? ApplicantName { get; set; }
    public string? ProgramName { get; set; }
    public string? EnrollmentYear { get; set; }
    public string? CampusName { get; set; }
    public string? AdmissionTypeName { get; set; }
    public string? Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? Notes { get; set; }
}
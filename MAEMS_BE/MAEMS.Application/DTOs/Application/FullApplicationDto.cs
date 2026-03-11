namespace MAEMS.Application.DTOs.Application;

public class FullApplicationDto
{
    public int ApplicationId { get; set; }
    public int? ApplicantId { get; set; }
    public int? ConfigId { get; set; }
    public int? ProgramId { get; set; }
    public int? CampusId { get; set; }
    public int? AdmissionTypeId { get; set; }
    public string? Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int? AssignedOfficerId { get; set; }
    public string? Notes { get; set; }
    public bool? RequiresReview { get; set; }
    public string? ApplicantName { get; set; }
    public string? ProgramName { get; set; }
    public string? CampusName { get; set; }
    public string? AdmissionTypeName { get; set; }
    public string? AssignedOfficerName { get; set; }
}
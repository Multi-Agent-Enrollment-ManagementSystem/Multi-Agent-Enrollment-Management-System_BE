namespace MAEMS.Application.DTOs.Application;

public class ApplicationBasicDto
{
    public int ApplicationId { get; set; }
    public int? ApplicantId { get; set; }
    public string? ApplicantName { get; set; }
    public int? ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public string? Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool? RequiresReview { get; set; }
}
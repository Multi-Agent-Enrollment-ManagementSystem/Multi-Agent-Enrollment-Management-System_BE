namespace MAEMS.Domain.Entities;

public class Applicant
{
    public int ApplicantId { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? HighSchoolName { get; set; }
    public string? HighSchoolDistrict { get; set; }
    public string? HighSchoolProvince { get; set; }
    public int? GraduationYear { get; set; }
    public string? IdIssueNumber { get; set; }
    public DateOnly? IdIssueDate { get; set; }
    public string? IdIssuePlace { get; set; }
    public string? ContactName { get; set; }
    public string? ContactAddress { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public bool? AllowShare { get; set; }
    public DateTime? CreatedAt { get; set; }
}

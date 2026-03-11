namespace MAEMS.Domain.Entities;

public class Program
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int? MajorId { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? EnrollmentYear { get; set; }
    public string? Description { get; set; }
    public string? CareerProspects { get; set; }
    public string? Duration { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

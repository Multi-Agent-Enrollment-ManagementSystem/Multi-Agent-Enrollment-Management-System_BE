namespace MAEMS.Domain.Entities;

public class EnrollmentYear
{
    public int EnrollmentYearId { get; set; }
    public string Year { get; set; } = string.Empty;
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

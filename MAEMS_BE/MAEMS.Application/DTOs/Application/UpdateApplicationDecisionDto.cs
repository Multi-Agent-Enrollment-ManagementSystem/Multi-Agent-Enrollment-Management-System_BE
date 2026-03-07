namespace MAEMS.Application.DTOs.Application;

public class UpdateApplicationDecisionDto
{
    public string Status { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
}
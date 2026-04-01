namespace MAEMS.Application.DTOs.Report;

public sealed class OfficerApplicationsCountDto
{
    public int? AssignedOfficerId { get; set; }
    public string? AssignedOfficerName { get; set; }
    public int Count { get; set; }
}

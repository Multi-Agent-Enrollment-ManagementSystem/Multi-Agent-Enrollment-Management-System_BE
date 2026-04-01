namespace MAEMS.Application.DTOs.Report;

public sealed class CampusApplicationsCountDto
{
    public int CampusId { get; set; }
    public string? CampusName { get; set; }
    public int Count { get; set; }
}

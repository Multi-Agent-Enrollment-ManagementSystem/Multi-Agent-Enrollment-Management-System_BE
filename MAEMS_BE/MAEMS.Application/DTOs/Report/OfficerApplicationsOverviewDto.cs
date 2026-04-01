using MAEMS.Application.DTOs.Application;

namespace MAEMS.Application.DTOs.Report;

public sealed class OfficerApplicationsOverviewDto
{
    public int TotalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    public List<FullApplicationDto> Applications { get; set; } = new();
}

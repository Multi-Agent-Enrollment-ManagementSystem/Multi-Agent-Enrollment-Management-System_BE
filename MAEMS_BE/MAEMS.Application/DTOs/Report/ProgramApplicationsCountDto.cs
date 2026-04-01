namespace MAEMS.Application.DTOs.Report;

public sealed class ProgramApplicationsCountDto
{
    public int ProgramId { get; set; }
    public string? ProgramName { get; set; }
    public int Count { get; set; }
}

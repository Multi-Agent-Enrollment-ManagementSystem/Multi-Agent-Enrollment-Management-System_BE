namespace MAEMS.Application.DTOs.Program;

public class ProgramDto
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CareerProspects { get; set; }
    public string? Duration { get; set; }
    public bool? IsActive { get; set; }
}

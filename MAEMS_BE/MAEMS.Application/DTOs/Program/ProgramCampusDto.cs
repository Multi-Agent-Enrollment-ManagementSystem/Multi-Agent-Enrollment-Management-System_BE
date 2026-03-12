namespace MAEMS.Application.DTOs.Program;

public class ProgramCampusDto
{
    public int? CampusId { get; set; }
    public string? CampusName { get; set; }
    public List<ProgramCampusAdmissionDto> Admissions { get; set; } = new();
}

namespace MAEMS.Application.DTOs.Major;

public class MajorDto
{
    public int MajorId { get; set; }
    public string MajorCode { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

namespace MAEMS.Domain.Entities;

public class Major
{
    public int MajorId { get; set; }
    public string MajorCode { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

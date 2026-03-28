namespace MAEMS.Domain.Entities;

public class Campus
{
    public int CampusId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

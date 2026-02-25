namespace MAEMS.Application.DTOs.Role;

public class RoleDto
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}

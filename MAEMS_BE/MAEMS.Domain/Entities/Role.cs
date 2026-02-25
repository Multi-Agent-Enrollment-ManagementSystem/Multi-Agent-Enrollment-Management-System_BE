namespace MAEMS.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}

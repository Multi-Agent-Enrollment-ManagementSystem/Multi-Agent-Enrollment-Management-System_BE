namespace MAEMS.Application.DTOs.User;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool? IsActive { get; set; }
}

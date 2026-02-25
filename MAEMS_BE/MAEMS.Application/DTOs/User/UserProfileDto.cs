namespace MAEMS.Application.DTOs.User;

public class UserProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

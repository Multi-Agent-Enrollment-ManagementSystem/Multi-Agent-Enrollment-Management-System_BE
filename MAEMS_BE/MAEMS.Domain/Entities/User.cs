namespace MAEMS.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public bool? IsActive { get; set; }
}

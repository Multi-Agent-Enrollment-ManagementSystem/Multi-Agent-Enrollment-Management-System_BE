namespace MAEMS.Application.DTOs.User;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public LoginUserDto User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}

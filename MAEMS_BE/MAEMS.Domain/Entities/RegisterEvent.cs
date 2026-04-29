namespace MAEMS.Domain.Entities;

public class RegisterEvent
{
    public int RegisterId { get; set; }
    public int ArticleId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? CreatedAt { get; set; }
}
using System;

namespace MAEMS.Domain.Entities;

public class Feedback
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }

    public User? User { get; set; }
}
using System;

namespace MAEMS.Application.DTOs.Feedback;

public class FeedbackDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}
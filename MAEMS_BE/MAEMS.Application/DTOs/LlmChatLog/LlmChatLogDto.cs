namespace MAEMS.Application.DTOs.LlmChatLog;

public class LlmChatLogDto
{
    public int ChatId { get; set; }

    public int? UserId { get; set; }

    public string UserQuery { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string LlmResponse { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
}


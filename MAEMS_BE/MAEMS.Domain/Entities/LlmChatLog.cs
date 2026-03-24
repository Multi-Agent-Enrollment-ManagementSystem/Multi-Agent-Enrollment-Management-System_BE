namespace MAEMS.Domain.Entities;

/// <summary>
/// LlmChatLog entity represents a chat conversation with LLM model.
/// Mirrors MAEMS.Infrastructure.Models.LlmChatLog (EF generated).
/// </summary>
public class LlmChatLog
{
    public int ChatId { get; set; }

    public int? UserId { get; set; }

    public string UserQuery { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string LlmResponse { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
}

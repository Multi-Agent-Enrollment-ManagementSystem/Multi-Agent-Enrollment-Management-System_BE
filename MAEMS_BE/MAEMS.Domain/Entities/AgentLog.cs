namespace MAEMS.Domain.Entities;

/// <summary>
/// AgentLog entity used by agents to persist their actions and raw outputs.
/// Mirrors MAEMS.Infrastructure.Models.AgentLog (EF generated).
/// </summary>
public class AgentLog
{
    public int LogId { get; set; }

    public int? ApplicationId { get; set; }

    public int? DocumentId { get; set; }

    public string AgentType { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? OutputData { get; set; }

    public DateTime? CreatedAt { get; set; }
}

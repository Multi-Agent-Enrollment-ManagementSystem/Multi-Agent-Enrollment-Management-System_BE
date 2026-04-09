namespace MAEMS.Application.DTOs.AgentLog;

public class AgentLogDto
{
    public int LogId { get; set; }

    public int? ApplicationId { get; set; }

    public int? ApplicantId { get; set; }

    public int? DocumentId { get; set; }

    public string AgentType { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? OutputData { get; set; }

    public DateTime? CreatedAt { get; set; }
}

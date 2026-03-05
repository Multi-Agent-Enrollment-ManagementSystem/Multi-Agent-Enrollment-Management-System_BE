using System.Text.Json.Serialization;

namespace MAEMS.MultiAgent.Agents;

// ── LLM eligibility payload (inner JSON trong content) ───────────────────────

/// <summary>
/// JSON payload mà LLM trả về bên trong <c>message.content</c> cho eligibility task.
/// </summary>
internal sealed class LlmEligibilityResponse
{
    [JsonPropertyName("result")]
    public string Result { get; init; } = "rejected";

    [JsonPropertyName("details")]
    public string? Details { get; init; }
}

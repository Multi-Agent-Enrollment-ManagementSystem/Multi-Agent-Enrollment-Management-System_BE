using System.Text.Json.Serialization;

namespace MAEMS.MultiAgent.Agents;

// ── LLM verification payload (inner JSON trong content) ──────────────────────

/// <summary>
/// JSON payload mà LLM trả về bên trong <c>message.content</c> cho verification task.
/// </summary>
internal sealed class LlmVerificationResponse
{
    [JsonPropertyName("result")]
    public string Result { get; init; } = "rejected";

    /// <summary>Null khi result = "verified".</summary>
    [JsonPropertyName("details")]
    public string? Details { get; init; }
}

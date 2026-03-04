using System.Text.Json.Serialization;

namespace MAEMS.MultiAgent.Agents;

// ── Ollama request models ─────────────────────────────────────────────────────

/// <summary>
/// Root request body gửi tới Ollama /api/chat.
/// </summary>
internal sealed class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("stream")]
    public bool Stream { get; init; } = false;

    [JsonPropertyName("messages")]
    public required IReadOnlyList<OllamaMessage> Messages { get; init; }
}

/// <summary>
/// Một message trong cuộc hội thoại Ollama.
/// </summary>
internal sealed class OllamaMessage
{
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>
    /// Danh sách ảnh base64 đính kèm (raw base64, không có "data:...;base64," prefix).
    /// Chỉ set cho user message có ảnh/PDF đã render.
    /// </summary>
    [JsonPropertyName("images")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Images { get; init; }
}

// ── Ollama response models ────────────────────────────────────────────────────

/// <summary>
/// Response trả về từ Ollama /api/chat (native envelope).
/// </summary>
internal sealed class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaResponseMessage? Message { get; init; }

    /// <summary>OpenAI-compatible fallback.</summary>
    [JsonPropertyName("choices")]
    public IReadOnlyList<OllamaChoice>? Choices { get; init; }
}

internal sealed class OllamaResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}

internal sealed class OllamaChoice
{
    [JsonPropertyName("message")]
    public OllamaResponseMessage? Message { get; init; }
}

// ── LLM quality-check payload (inner JSON trong content) ─────────────────────

/// <summary>
/// JSON payload mà LLM trả về bên trong <c>message.content</c>.
/// </summary>
internal sealed class LlmQualityCheckResponse
{
    [JsonPropertyName("document_type")]
    public string? DocumentType { get; init; }

    [JsonPropertyName("passed_quality_check")]
    public bool PassedQualityCheck { get; init; }

    [JsonPropertyName("quality")]
    public LlmQuality? Quality { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    [JsonPropertyName("issues")]
    public List<string>? Issues { get; init; }
}

internal sealed class LlmQuality
{
    [JsonPropertyName("is_readable")]
    public bool IsReadable { get; init; }

    [JsonPropertyName("is_unobscured")]
    public bool IsUnobscured { get; init; }

    [JsonPropertyName("is_unblurred")]
    public bool IsUnblurred { get; init; }

    [JsonPropertyName("is_complete")]
    public bool IsComplete { get; init; }

    [JsonPropertyName("is_unedited")]
    public bool IsUnedited { get; init; }
}

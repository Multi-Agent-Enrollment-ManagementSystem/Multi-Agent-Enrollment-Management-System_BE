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

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("details")]
    public string? Details { get; init; }

    // HK2 scores
    [JsonPropertyName("hk2_math")]
    public decimal? Hk2Math { get; init; }

    [JsonPropertyName("hk2_literature")]
    public decimal? Hk2Literature { get; init; }

    [JsonPropertyName("hk2_foreign_language")]
    public decimal? Hk2ForeignLanguage { get; init; }

    [JsonPropertyName("hk2_history")]
    public decimal? Hk2History { get; init; }

    [JsonPropertyName("hk2_physics")]
    public decimal? Hk2Physics { get; init; }

    [JsonPropertyName("hk2_chemistry")]
    public decimal? Hk2Chemistry { get; init; }

    [JsonPropertyName("hk2_biology")]
    public decimal? Hk2Biology { get; init; }

    [JsonPropertyName("hk2_geography")]
    public decimal? Hk2Geography { get; init; }

    [JsonPropertyName("hk2_economics_law")]
    public decimal? Hk2EconomicsLaw { get; init; }

    [JsonPropertyName("hk2_informatics")]
    public decimal? Hk2Informatics { get; init; }

    [JsonPropertyName("hk2_technology")]
    public decimal? Hk2Technology { get; init; }

    // THPT scores
    [JsonPropertyName("thpt_math")]
    public decimal? ThptMath { get; init; }

    [JsonPropertyName("thpt_literature")]
    public decimal? ThptLiterature { get; init; }

    [JsonPropertyName("thpt_foreign_language")]
    public decimal? ThptForeignLanguage { get; init; }

    [JsonPropertyName("thpt_history")]
    public decimal? ThptHistory { get; init; }

    [JsonPropertyName("thpt_geography")]
    public decimal? ThptGeography { get; init; }

    [JsonPropertyName("thpt_physics")]
    public decimal? ThptPhysics { get; init; }

    [JsonPropertyName("thpt_chemistry")]
    public decimal? ThptChemistry { get; init; }

    [JsonPropertyName("thpt_biology")]
    public decimal? ThptBiology { get; init; }

    [JsonPropertyName("thpt_economics_law")]
    public decimal? ThptEconomicsLaw { get; init; }

    [JsonPropertyName("thpt_informatics")]
    public decimal? ThptInformatics { get; init; }

    [JsonPropertyName("thpt_technology")]
    public decimal? ThptTechnology { get; init; }

    [JsonPropertyName("dgnl")]
    public decimal? Dgnl { get; init; }
}

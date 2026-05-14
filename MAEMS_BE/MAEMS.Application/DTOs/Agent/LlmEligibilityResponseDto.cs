namespace MAEMS.Application.DTOs.Agent;

public sealed class LlmEligibilityResponseDto
{
    public string Result { get; init; } = "rejected";
    public string? Level { get; init; }
    public string? Details { get; init; }

    public decimal? Hk2Math { get; init; }
    public decimal? Hk2Literature { get; init; }
    public decimal? Hk2ForeignLanguage { get; init; }
    public decimal? Hk2History { get; init; }
    public decimal? Hk2Physics { get; init; }
    public decimal? Hk2Chemistry { get; init; }
    public decimal? Hk2Biology { get; init; }
    public decimal? Hk2Geography { get; init; }
    public decimal? Hk2EconomicsLaw { get; init; }
    public decimal? Hk2Informatics { get; init; }
    public decimal? Hk2Technology { get; init; }

    public decimal? ThptMath { get; init; }
    public decimal? ThptLiterature { get; init; }
    public decimal? ThptForeignLanguage { get; init; }
    public decimal? ThptHistory { get; init; }
    public decimal? ThptGeography { get; init; }
    public decimal? ThptPhysics { get; init; }
    public decimal? ThptChemistry { get; init; }
    public decimal? ThptBiology { get; init; }
    public decimal? ThptEconomicsLaw { get; init; }
    public decimal? ThptInformatics { get; init; }
    public decimal? ThptTechnology { get; init; }

    public decimal? Dgnl { get; init; }
}
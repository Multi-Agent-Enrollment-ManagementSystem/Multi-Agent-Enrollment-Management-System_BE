namespace MAEMS.Application.DTOs.Score;

public class ScoreDto
{
    public int ScoreId { get; set; }
    public int ApplicantId { get; set; }

    public decimal? Hk2Math { get; set; }
    public decimal? Hk2Literature { get; set; }
    public decimal? Hk2ForeignLanguage { get; set; }
    public decimal? Hk2History { get; set; }
    public decimal? Hk2Geography { get; set; }
    public decimal? Hk2Physics { get; set; }
    public decimal? Hk2Chemistry { get; set; }
    public decimal? Hk2Biology { get; set; }
    public decimal? Hk2EconomicsLaw { get; set; }
    public decimal? Hk2Informatics { get; set; }
    public decimal? Hk2Technology { get; set; }

    public decimal? ThptMath { get; set; }
    public decimal? ThptLiterature { get; set; }
    public decimal? ThptForeignLanguage { get; set; }
    public decimal? ThptHistory { get; set; }
    public decimal? ThptGeography { get; set; }
    public decimal? ThptPhysics { get; set; }
    public decimal? ThptChemistry { get; set; }
    public decimal? ThptBiology { get; set; }
    public decimal? ThptEconomicsLaw { get; set; }
    public decimal? ThptInformatics { get; set; }
    public decimal? ThptTechnology { get; set; }
    
    public decimal? Dgnl { get; set; }
}

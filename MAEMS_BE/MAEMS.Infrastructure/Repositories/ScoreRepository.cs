using System.Threading.Tasks;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainScore = MAEMS.Domain.Entities.Score;
using InfraScore = MAEMS.Infrastructure.Models.Score;

namespace MAEMS.Infrastructure.Repositories;

public class ScoreRepository : Domain.Interfaces.IScoreRepository
{
    private readonly postgresContext _context;

    public ScoreRepository(postgresContext context)
    {
        _context = context;
    }

    public async Task<DomainScore?> GetByApplicantIdAsync(int applicantId)
    {
        var infraScore = await _context.Scores.FirstOrDefaultAsync(s => s.ApplicantId == applicantId);
        
        if (infraScore == null)
            return null;

        return MapToDomain(infraScore);
    }

    public async Task<DomainScore> AddAsync(DomainScore score)
    {
        var infraScore = MapToInfra(score);
        _context.Scores.Add(infraScore);
        await _context.SaveChangesAsync();
        score.ScoreId = infraScore.ScoreId;
        return score;
    }

    public async Task UpdateAsync(DomainScore score)
    {
        var infraScore = await _context.Scores.FindAsync(score.ScoreId);
        if (infraScore != null)
        {
            // Update properties mapping
            infraScore.ApplicantId = score.ApplicantId;
            infraScore.Hk2Math = score.Hk2Math;
            infraScore.Hk2Literature = score.Hk2Literature;
            infraScore.Hk2ForeignLanguage = score.Hk2ForeignLanguage;
            infraScore.Hk2History = score.Hk2History;
            infraScore.Hk2Geography = score.Hk2Geography;
            infraScore.Hk2Physics = score.Hk2Physics;
            infraScore.Hk2Chemistry = score.Hk2Chemistry;
            infraScore.Hk2Biology = score.Hk2Biology;
            infraScore.Hk2EconomicsLaw = score.Hk2EconomicsLaw;
            infraScore.Hk2Informatics = score.Hk2Informatics;
            infraScore.Hk2Technology = score.Hk2Technology;
            infraScore.ThptMath = score.ThptMath;
            infraScore.ThptLiterature = score.ThptLiterature;
            infraScore.ThptForeignLanguage = score.ThptForeignLanguage;
            infraScore.ThptHistory = score.ThptHistory;
            infraScore.ThptGeography = score.ThptGeography;
            infraScore.ThptPhysics = score.ThptPhysics;
            infraScore.ThptChemistry = score.ThptChemistry;
            infraScore.ThptBiology = score.ThptBiology;
            infraScore.ThptEconomicsLaw = score.ThptEconomicsLaw;
            infraScore.ThptInformatics = score.ThptInformatics;
            infraScore.ThptTechnology = score.ThptTechnology;
            infraScore.Dgnl = score.Dgnl;

            await _context.SaveChangesAsync();
        }
    }

    private static DomainScore MapToDomain(InfraScore infraScore)
    {
        return new DomainScore
        {
            ScoreId = infraScore.ScoreId,
            ApplicantId = infraScore.ApplicantId,
            Hk2Math = infraScore.Hk2Math,
            Hk2Literature = infraScore.Hk2Literature,
            Hk2ForeignLanguage = infraScore.Hk2ForeignLanguage,
            Hk2History = infraScore.Hk2History,
            Hk2Geography = infraScore.Hk2Geography,
            Hk2Physics = infraScore.Hk2Physics,
            Hk2Chemistry = infraScore.Hk2Chemistry,
            Hk2Biology = infraScore.Hk2Biology,
            Hk2EconomicsLaw = infraScore.Hk2EconomicsLaw,
            Hk2Informatics = infraScore.Hk2Informatics,
            Hk2Technology = infraScore.Hk2Technology,
            ThptMath = infraScore.ThptMath,
            ThptLiterature = infraScore.ThptLiterature,
            ThptForeignLanguage = infraScore.ThptForeignLanguage,
            ThptHistory = infraScore.ThptHistory,
            ThptGeography = infraScore.ThptGeography,
            ThptPhysics = infraScore.ThptPhysics,
            ThptChemistry = infraScore.ThptChemistry,
            ThptBiology = infraScore.ThptBiology,
            ThptEconomicsLaw = infraScore.ThptEconomicsLaw,
            ThptInformatics = infraScore.ThptInformatics,
            ThptTechnology = infraScore.ThptTechnology,
            Dgnl = infraScore.Dgnl
        };
    }

    private static InfraScore MapToInfra(DomainScore domainScore)
    {
        return new InfraScore
        {
            ScoreId = domainScore.ScoreId,
            ApplicantId = domainScore.ApplicantId,
            Hk2Math = domainScore.Hk2Math,
            Hk2Literature = domainScore.Hk2Literature,
            Hk2ForeignLanguage = domainScore.Hk2ForeignLanguage,
            Hk2History = domainScore.Hk2History,
            Hk2Geography = domainScore.Hk2Geography,
            Hk2Physics = domainScore.Hk2Physics,
            Hk2Chemistry = domainScore.Hk2Chemistry,
            Hk2Biology = domainScore.Hk2Biology,
            Hk2EconomicsLaw = domainScore.Hk2EconomicsLaw,
            Hk2Informatics = domainScore.Hk2Informatics,
            Hk2Technology = domainScore.Hk2Technology,
            ThptMath = domainScore.ThptMath,
            ThptLiterature = domainScore.ThptLiterature,
            ThptForeignLanguage = domainScore.ThptForeignLanguage,
            ThptHistory = domainScore.ThptHistory,
            ThptGeography = domainScore.ThptGeography,
            ThptPhysics = domainScore.ThptPhysics,
            ThptChemistry = domainScore.ThptChemistry,
            ThptBiology = domainScore.ThptBiology,
            ThptEconomicsLaw = domainScore.ThptEconomicsLaw,
            ThptInformatics = domainScore.ThptInformatics,
            ThptTechnology = domainScore.ThptTechnology,
            Dgnl = domainScore.Dgnl
        };
    }
}
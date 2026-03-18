using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainEnrollmentYear = MAEMS.Domain.Entities.EnrollmentYear;
using InfraEnrollmentYear = MAEMS.Infrastructure.Models.EnrollmentYear;

namespace MAEMS.Infrastructure.Repositories;

public class EnrollmentYearRepository : BaseRepository, IEnrollmentYearRepository
{
    public EnrollmentYearRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainEnrollmentYear?> GetByIdAsync(int id)
    {
        var infra = await _context.EnrollmentYears.FirstOrDefaultAsync(x => x.EnrollmentYearId == id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainEnrollmentYear>> GetAllAsync()
    {
        var infra = await _context.EnrollmentYears.ToListAsync();
        return infra.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainEnrollmentYear>> FindAsync(System.Linq.Expressions.Expression<Func<DomainEnrollmentYear, bool>> predicate)
    {
        var all = (await GetAllAsync()).ToList();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainEnrollmentYear> AddAsync(DomainEnrollmentYear entity)
    {
        var infra = new InfraEnrollmentYear
        {
            Year = entity.Year,
            RegistrationStartDate = entity.RegistrationStartDate,
            RegistrationEndDate = entity.RegistrationEndDate,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt
        };

        await _context.EnrollmentYears.AddAsync(infra);
        entity.EnrollmentYearId = infra.EnrollmentYearId;
        return entity;
    }

    public async Task UpdateAsync(DomainEnrollmentYear entity)
    {
        var infra = await _context.EnrollmentYears.FindAsync(entity.EnrollmentYearId);
        if (infra != null)
        {
            infra.Year = entity.Year;
            infra.RegistrationStartDate = entity.RegistrationStartDate;
            infra.RegistrationEndDate = entity.RegistrationEndDate;
            infra.Status = entity.Status;
            _context.EnrollmentYears.Update(infra);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainEnrollmentYear entity)
    {
        var infra = await _context.EnrollmentYears.FindAsync(entity.EnrollmentYearId);
        if (infra != null)
        {
            _context.EnrollmentYears.Remove(infra);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<DomainEnrollmentYear, bool>> predicate)
    {
        var all = (await GetAllAsync()).ToList();
        return all.Any(predicate.Compile());
    }

    private static DomainEnrollmentYear MapToDomain(InfraEnrollmentYear infra)
    {
        return new DomainEnrollmentYear
        {
            EnrollmentYearId = infra.EnrollmentYearId,
            Year = infra.Year ?? string.Empty,
            RegistrationStartDate = infra.RegistrationStartDate,
            RegistrationEndDate = infra.RegistrationEndDate,
            Status = infra.Status ?? string.Empty,
            CreatedAt = infra.CreatedAt
        };
    }
}

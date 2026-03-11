using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainConfig = MAEMS.Domain.Entities.ProgramAdmissionConfig;
using InfraConfig = MAEMS.Infrastructure.Models.ProgramAdmissionConfig;

namespace MAEMS.Infrastructure.Repositories;

public class ProgramAdmissionConfigRepository : BaseRepository, IProgramAdmissionConfigRepository
{
    public ProgramAdmissionConfigRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainConfig?> GetByIdAsync(int id)
    {
        var infra = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .FirstOrDefaultAsync(c => c.ConfigId == id);

        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainConfig>> GetAllAsync()
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .ToListAsync();

        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainConfig>> GetActiveConfigsAsync()
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .Where(c => c.IsActive == true)
            .ToListAsync();

        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainConfig>> GetConfigsByProgramIdAsync(int programId)
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .Where(c => c.ProgramId == programId)
            .ToListAsync();

        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainConfig>> GetConfigsByCampusIdAsync(int campusId)
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .Where(c => c.CampusId == campusId)
            .ToListAsync();

        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainConfig>> GetConfigsByAdmissionTypeIdAsync(int admissionTypeId)
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .Where(c => c.AdmissionTypeId == admissionTypeId)
            .ToListAsync();

        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainConfig>> FindAsync(Expression<Func<DomainConfig, bool>> predicate)
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .ToListAsync();

        return infraList.Select(MapToDomain).Where(predicate.Compile());
    }

    public async Task<DomainConfig> AddAsync(DomainConfig entity)
    {
        var infra = new InfraConfig
        {
            ProgramId = entity.ProgramId,
            CampusId = entity.CampusId,
            AdmissionTypeId = entity.AdmissionTypeId,
            Quota = entity.Quota,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        await _context.ProgramAdmissionConfigs.AddAsync(infra);
        entity.ConfigId = infra.ConfigId;
        return entity;
    }

    public async Task UpdateAsync(DomainConfig entity)
    {
        var infra = await _context.ProgramAdmissionConfigs.FindAsync(entity.ConfigId);
        if (infra != null)
        {
            infra.ProgramId = entity.ProgramId;
            infra.CampusId = entity.CampusId;
            infra.AdmissionTypeId = entity.AdmissionTypeId;
            infra.Quota = entity.Quota;
            infra.IsActive = entity.IsActive;
            _context.ProgramAdmissionConfigs.Update(infra);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainConfig entity)
    {
        var infra = await _context.ProgramAdmissionConfigs.FindAsync(entity.ConfigId);
        if (infra != null)
        {
            _context.ProgramAdmissionConfigs.Remove(infra);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainConfig, bool>> predicate)
    {
        var infraList = await _context.ProgramAdmissionConfigs
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .ToListAsync();

        return infraList.Select(MapToDomain).Any(predicate.Compile());
    }

    private static DomainConfig MapToDomain(InfraConfig infra)
    {
        return new DomainConfig
        {
            ConfigId = infra.ConfigId,
            ProgramId = infra.ProgramId,
            ProgramName = infra.Program?.ProgramName,
            CampusId = infra.CampusId,
            CampusName = infra.Campus?.Name,
            AdmissionTypeId = infra.AdmissionTypeId,
            AdmissionTypeName = infra.AdmissionType?.AdmissionTypeName,
            Quota = infra.Quota,
            IsActive = infra.IsActive,
            CreatedAt = infra.CreatedAt
        };
    }
}

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

    public async Task<(IReadOnlyList<DomainConfig> Items, int TotalCount)> GetConfigsPagedAsync(
        int? programId,
        int? campusId,
        int? admissionTypeId,
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.ProgramAdmissionConfigs
            .AsNoTracking()
            .Include(c => c.Program)
            .Include(c => c.Campus)
            .Include(c => c.AdmissionType)
            .AsQueryable();

        if (programId.HasValue)
            query = query.Where(c => c.ProgramId == programId.Value);

        if (campusId.HasValue)
            query = query.Where(c => c.CampusId == campusId.Value);

        if (admissionTypeId.HasValue)
            query = query.Where(c => c.AdmissionTypeId == admissionTypeId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                EF.Functions.ILike(c.Program!.ProgramName!, $"%{search}%") ||
                EF.Functions.ILike(c.Campus!.Name!, $"%{search}%") ||
                EF.Functions.ILike(c.AdmissionType!.AdmissionTypeName!, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "configId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "configid" => sortDesc ? query.OrderByDescending(x => x.ConfigId) : query.OrderBy(x => x.ConfigId),
            "programname" => sortDesc ? query.OrderByDescending(x => x.Program!.ProgramName) : query.OrderBy(x => x.Program!.ProgramName),
            "campusname" => sortDesc ? query.OrderByDescending(x => x.Campus!.Name) : query.OrderBy(x => x.Campus!.Name),
            "admissiontypename" => sortDesc ? query.OrderByDescending(x => x.AdmissionType!.AdmissionTypeName) : query.OrderBy(x => x.AdmissionType!.AdmissionTypeName),
            "quota" => sortDesc ? query.OrderByDescending(x => x.Quota) : query.OrderBy(x => x.Quota),
            "isactive" => sortDesc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            "createdat" => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(x => x.ConfigId) : query.OrderBy(x => x.ConfigId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
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

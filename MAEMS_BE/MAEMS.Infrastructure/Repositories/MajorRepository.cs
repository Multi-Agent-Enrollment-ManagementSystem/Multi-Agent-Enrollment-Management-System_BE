using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainMajor = MAEMS.Domain.Entities.Major;
using InfraMajor = MAEMS.Infrastructure.Models.Major;

namespace MAEMS.Infrastructure.Repositories;

public class MajorRepository : BaseRepository, IMajorRepository
{
    public MajorRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainMajor?> GetByMajorCodeAsync(string majorCode)
    {
        var infraMajor = await _context.Majors.FirstOrDefaultAsync(m => m.MajorCode == majorCode);
        
        if (infraMajor == null)
            return null;

        return MapToDomain(infraMajor);
    }

    public async Task<IEnumerable<DomainMajor>> GetActiveMajorsAsync()
    {
        var infraMajors = await _context.Majors
            .Where(m => m.IsActive == true)
            .ToListAsync();

        return infraMajors.Select(MapToDomain);
    }

    public async Task<DomainMajor?> GetByIdAsync(int id)
    {
        var infraMajor = await _context.Majors.FindAsync(id);
        
        if (infraMajor == null)
            return null;

        return MapToDomain(infraMajor);
    }

    public async Task<IEnumerable<DomainMajor>> GetAllAsync()
    {
        var infraMajors = await _context.Majors.ToListAsync();
        return infraMajors.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainMajor>> FindAsync(Expression<Func<DomainMajor, bool>> predicate)
    {
        var infraMajors = await _context.Majors.ToListAsync();
        var domainMajors = infraMajors.Select(MapToDomain);
        return domainMajors.Where(predicate.Compile());
    }

    public async Task<DomainMajor> AddAsync(DomainMajor entity)
    {
        var infraMajor = new InfraMajor
        {
            MajorCode = entity.MajorCode,
            MajorName = entity.MajorName,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        await _context.Majors.AddAsync(infraMajor);
        
        entity.MajorId = infraMajor.MajorId;
        return entity;
    }

    public async Task UpdateAsync(DomainMajor entity)
    {
        var infraMajor = await _context.Majors.FindAsync(entity.MajorId);
        if (infraMajor != null)
        {
            infraMajor.MajorCode = entity.MajorCode;
            infraMajor.MajorName = entity.MajorName;
            infraMajor.Description = entity.Description;
            infraMajor.IsActive = entity.IsActive;

            _context.Majors.Update(infraMajor);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainMajor entity)
    {
        var infraMajor = await _context.Majors.FindAsync(entity.MajorId);
        if (infraMajor != null)
        {
            _context.Majors.Remove(infraMajor);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainMajor, bool>> predicate)
    {
        var infraMajors = await _context.Majors.ToListAsync();
        var domainMajors = infraMajors.Select(MapToDomain);
        return domainMajors.Any(predicate.Compile());
    }

    public async Task<(IReadOnlyList<DomainMajor> Items, int TotalCount)> GetMajorsPagedAsync(
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

        var query = _context.Majors.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m =>
                EF.Functions.ILike(m.MajorCode!, $"%{search}%") ||
                EF.Functions.ILike(m.MajorName!, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "majorId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "majorid" => sortDesc ? query.OrderByDescending(x => x.MajorId) : query.OrderBy(x => x.MajorId),
            "majorcode" => sortDesc ? query.OrderByDescending(x => x.MajorCode) : query.OrderBy(x => x.MajorCode),
            "majorname" => sortDesc ? query.OrderByDescending(x => x.MajorName) : query.OrderBy(x => x.MajorName),
            "createdat" => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            "isactive" => sortDesc ? query.OrderByDescending(x => x.IsActive) : query.OrderBy(x => x.IsActive),
            _ => sortDesc ? query.OrderByDescending(x => x.MajorId) : query.OrderBy(x => x.MajorId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    private static DomainMajor MapToDomain(InfraMajor infraMajor)
    {
        return new DomainMajor
        {
            MajorId = infraMajor.MajorId,
            MajorCode = infraMajor.MajorCode ?? string.Empty,
            MajorName = infraMajor.MajorName ?? string.Empty,
            Description = infraMajor.Description,
            IsActive = infraMajor.IsActive,
            CreatedAt = infraMajor.CreatedAt
        };
    }
}

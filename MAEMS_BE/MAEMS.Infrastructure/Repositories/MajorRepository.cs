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

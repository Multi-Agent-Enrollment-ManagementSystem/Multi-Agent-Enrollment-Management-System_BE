using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainCampus = MAEMS.Domain.Entities.Campus;
using InfraCampus = MAEMS.Infrastructure.Models.Campus;

namespace MAEMS.Infrastructure.Repositories;

public class CampusRepository : BaseRepository, ICampusRepository
{
    public CampusRepository(postgresContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DomainCampus>> GetActiveCampusesAsync()
    {
        var infraCampuses = await _context.Campuses
            .Where(c => c.IsActive == true)
            .ToListAsync();

        return infraCampuses.Select(MapToDomain);
    }

    public async Task<DomainCampus?> GetByIdAsync(int id)
    {
        var infraCampus = await _context.Campuses.FindAsync(id);
        
        if (infraCampus == null)
            return null;

        return MapToDomain(infraCampus);
    }

    public async Task<IEnumerable<DomainCampus>> GetAllAsync()
    {
        var infraCampuses = await _context.Campuses.ToListAsync();
        return infraCampuses.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainCampus>> FindAsync(Expression<Func<DomainCampus, bool>> predicate)
    {
        var infraCampuses = await _context.Campuses.ToListAsync();
        var domainCampuses = infraCampuses.Select(MapToDomain);
        return domainCampuses.Where(predicate.Compile());
    }

    public async Task<DomainCampus> AddAsync(DomainCampus entity)
    {
        var infraCampus = new InfraCampus
        {
            Name = entity.Name,
            Address = entity.Address,
            Email = entity.Email,
            PhoneNumber = entity.PhoneNumber,
            Description = entity.Description,
            IsActive = entity.IsActive
        };

        await _context.Campuses.AddAsync(infraCampus);

        entity.CampusId = infraCampus.CampusId;
        return entity;
    }

    public async Task UpdateAsync(DomainCampus entity)
    {
        var infraCampus = await _context.Campuses.FindAsync(entity.CampusId);
        if (infraCampus != null)
        {
            infraCampus.Name = entity.Name;
            infraCampus.Address = entity.Address;
            infraCampus.Email = entity.Email;
            infraCampus.PhoneNumber = entity.PhoneNumber;
            infraCampus.Description = entity.Description;
            infraCampus.IsActive = entity.IsActive;

            _context.Campuses.Update(infraCampus);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainCampus entity)
    {
        var infraCampus = await _context.Campuses.FindAsync(entity.CampusId);
        if (infraCampus != null)
        {
            _context.Campuses.Remove(infraCampus);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainCampus, bool>> predicate)
    {
        var infraCampuses = await _context.Campuses.ToListAsync();
        var domainCampuses = infraCampuses.Select(MapToDomain);
        return domainCampuses.Any(predicate.Compile());
    }

    private static DomainCampus MapToDomain(InfraCampus infraCampus)
    {
        return new DomainCampus
        {
            CampusId = infraCampus.CampusId,
            Name = infraCampus.Name ?? string.Empty,
            Address = infraCampus.Address,
            Email = infraCampus.Email,
            PhoneNumber = infraCampus.PhoneNumber,
            Description = infraCampus.Description,
            IsActive = infraCampus.IsActive
        };
    }
}

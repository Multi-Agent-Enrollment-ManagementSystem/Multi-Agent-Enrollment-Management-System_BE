using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainRole = MAEMS.Domain.Entities.Role;
using InfraRole = MAEMS.Infrastructure.Models.Role;

namespace MAEMS.Infrastructure.Repositories;

public class RoleRepository : BaseRepository, IRoleRepository
{
    public RoleRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainRole?> GetByNameAsync(string name)
    {
        var infraRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        
        if (infraRole == null)
            return null;

        return MapToDomain(infraRole);
    }

    public async Task<IEnumerable<DomainRole>> GetActiveRolesAsync()
    {
        var infraRoles = await _context.Roles
            .Where(r => r.IsActive == true)
            .ToListAsync();

        return infraRoles.Select(MapToDomain);
    }

    public async Task<DomainRole?> GetByIdAsync(int id)
    {
        var infraRole = await _context.Roles.FindAsync(id);
        
        if (infraRole == null)
            return null;

        return MapToDomain(infraRole);
    }

    public async Task<IEnumerable<DomainRole>> GetAllAsync()
    {
        var infraRoles = await _context.Roles.ToListAsync();
        return infraRoles.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainRole>> FindAsync(Expression<Func<DomainRole, bool>> predicate)
    {
        var infraRoles = await _context.Roles.ToListAsync();
        var domainRoles = infraRoles.Select(MapToDomain);
        return domainRoles.Where(predicate.Compile());
    }

    public async Task<DomainRole> AddAsync(DomainRole entity)
    {
        var infraRole = new InfraRole
        {
            Name = entity.Name,
            IsActive = entity.IsActive
        };

        await _context.Roles.AddAsync(infraRole);
        
        entity.RoleId = infraRole.RoleId;
        return entity;
    }

    public async Task UpdateAsync(DomainRole entity)
    {
        var infraRole = await _context.Roles.FindAsync(entity.RoleId);
        if (infraRole != null)
        {
            infraRole.Name = entity.Name;
            infraRole.IsActive = entity.IsActive;

            _context.Roles.Update(infraRole);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainRole entity)
    {
        var infraRole = await _context.Roles.FindAsync(entity.RoleId);
        if (infraRole != null)
        {
            _context.Roles.Remove(infraRole);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainRole, bool>> predicate)
    {
        var infraRoles = await _context.Roles.ToListAsync();
        var domainRoles = infraRoles.Select(MapToDomain);
        return domainRoles.Any(predicate.Compile());
    }

    private static DomainRole MapToDomain(InfraRole infraRole)
    {
        return new DomainRole
        {
            RoleId = infraRole.RoleId,
            Name = infraRole.Name ?? string.Empty,
            IsActive = infraRole.IsActive
        };
    }
}

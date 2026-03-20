using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainUser = MAEMS.Domain.Entities.User;
using InfraUser = MAEMS.Infrastructure.Models.User;

namespace MAEMS.Infrastructure.Repositories;

public class UserRepository : BaseRepository, IUserRepository
{
    public UserRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainUser?> GetByUsernameAsync(string username)
    {
        var infraUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (infraUser == null) return null;

        return MapToDomain(infraUser);
    }

    public async Task<DomainUser?> GetByEmailAsync(string email)
    {
        var infraUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (infraUser == null) return null;

        return MapToDomain(infraUser);
    }

    public async Task<bool> IsUsernameExistsAsync(string username)
    {
        return await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<DomainUser?> GetByIdAsync(int id)
    {
        var infraUser = await _context.Users.FindAsync(id);
        
        if (infraUser == null)
            return null;

        return MapToDomain(infraUser);
    }

    public async Task<IEnumerable<DomainUser>> GetAllAsync()
    {
        var infraUsers = await _context.Users.ToListAsync();
        return infraUsers.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainUser>> FindAsync(Expression<Func<DomainUser, bool>> predicate)
    {
        // Load all users from database and apply predicate in memory
        // For better performance, consider adding specific methods for common queries
        var infraUsers = await _context.Users.ToListAsync();
        var domainUsers = infraUsers.Select(MapToDomain);
        return domainUsers.Where(predicate.Compile());
    }

    public async Task<DomainUser> AddAsync(DomainUser entity)
    {
        var infraUser = new InfraUser
        {
            Username = entity.Username,
            Email = entity.Email,
            PasswordHash = entity.PasswordHash,
            RoleId = entity.RoleId,
            CreatedAt = entity.CreatedAt,
            IsActive = entity.IsActive
        };

        await _context.Users.AddAsync(infraUser);
        
        entity.UserId = infraUser.UserId;
        return entity;
    }

    public async Task UpdateAsync(DomainUser entity)
    {
        var infraUser = await _context.Users.FindAsync(entity.UserId);
        if (infraUser != null)
        {
            infraUser.Username = entity.Username;
            infraUser.Email = entity.Email;
            infraUser.PasswordHash = entity.PasswordHash;
            infraUser.RoleId = entity.RoleId;
            infraUser.CreatedAt = entity.CreatedAt;
            infraUser.IsActive = entity.IsActive;

            _context.Users.Update(infraUser);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainUser entity)
    {
        var infraUser = await _context.Users.FindAsync(entity.UserId);
        if (infraUser != null)
        {
            _context.Users.Remove(infraUser);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainUser, bool>> predicate)
    {
        var infraUsers = await _context.Users.ToListAsync();
        var domainUsers = infraUsers.Select(MapToDomain);
        return domainUsers.Any(predicate.Compile());
    }

    public async Task<(IReadOnlyList<DomainUser> Items, int TotalCount)> GetUsersPagedAsync(
        int? roleId,
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

        var query = _context.Users.AsNoTracking().AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Search on username/email
            query = query.Where(u =>
                EF.Functions.ILike(u.Username!, $"%{search}%") ||
                EF.Functions.ILike(u.Email!, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "userId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "userid" => sortDesc ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
            "username" => sortDesc ? query.OrderByDescending(x => x.Username) : query.OrderBy(x => x.Username),
            "email" => sortDesc ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "createdat" => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    private static DomainUser MapToDomain(InfraUser infraUser)
    {
        return new DomainUser
        {
            UserId = infraUser.UserId,
            Username = infraUser.Username ?? string.Empty,
            PasswordHash = infraUser.PasswordHash ?? string.Empty,
            Email = infraUser.Email ?? string.Empty,
            RoleId = infraUser.RoleId,
            CreatedAt = infraUser.CreatedAt,
            IsActive = infraUser.IsActive
        };
    }
}

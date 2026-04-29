using System.Linq.Expressions;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainRegisterEvent = MAEMS.Domain.Entities.RegisterEvent;
using InfraRegisterEvent = MAEMS.Infrastructure.Models.RegisterEvent;

namespace MAEMS.Infrastructure.Repositories;

public class RegisterEventRepository : BaseRepository, IRegisterEventRepository
{
    public RegisterEventRepository(postgresContext context) : base(context) { }

    public async Task<DomainRegisterEvent?> GetByIdAsync(int id)
    {
        var infra = await _context.RegisterEvents.FindAsync(id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainRegisterEvent>> GetAllAsync()
    {
        var infra = await _context.RegisterEvents.AsNoTracking().ToListAsync();
        return infra.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainRegisterEvent>> GetByArticleIdAsync(int articleId)
    {
        var infra = await _context.RegisterEvents
            .AsNoTracking()
            .Where(e => e.ArticleId == articleId)
            .ToListAsync();
        
        return infra.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainRegisterEvent>> FindAsync(Expression<Func<DomainRegisterEvent, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainRegisterEvent> AddAsync(DomainRegisterEvent entity)
    {
        var infra = new InfraRegisterEvent
        {
            ArticleId = entity.ArticleId,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone,
            CreatedAt = entity.CreatedAt
        };

        await _context.RegisterEvents.AddAsync(infra);
        entity.RegisterId = infra.RegisterId;
        return entity;
    }

    public async Task UpdateAsync(DomainRegisterEvent entity)
    {
        var infra = await _context.RegisterEvents.FindAsync(entity.RegisterId);
        if (infra == null) return;

        infra.ArticleId = entity.ArticleId;
        infra.FullName = entity.FullName;
        infra.Email = entity.Email;
        infra.Phone = entity.Phone;

        _context.RegisterEvents.Update(infra);
    }

    public async Task DeleteAsync(DomainRegisterEvent entity)
    {
        var infra = await _context.RegisterEvents.FindAsync(entity.RegisterId);
        if (infra != null)
        {
            _context.RegisterEvents.Remove(infra);
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainRegisterEvent, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    private static DomainRegisterEvent MapToDomain(InfraRegisterEvent infra)
    {
        return new DomainRegisterEvent
        {
            RegisterId = infra.RegisterId,
            ArticleId = infra.ArticleId,
            FullName = infra.FullName,
            Email = infra.Email,
            Phone = infra.Phone,
            CreatedAt = infra.CreatedAt
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainFeedback = MAEMS.Domain.Entities.Feedback;

namespace MAEMS.Infrastructure.Repositories;

public class FeedbackRepository : IFeedbackRepository
{
    private readonly postgresContext _context;

    public FeedbackRepository(postgresContext context)
    {
        _context = context;
    }

    public async Task<DomainFeedback?> GetByIdAsync(int id)
    {
        var infraFeedback = await _context.Feedbacks.FindAsync(id);
        return infraFeedback != null ? MapToDomain(infraFeedback) : null;
    }

    public async Task<IEnumerable<DomainFeedback>> GetAllAsync()
    {
        var infraFeedbacks = await _context.Feedbacks.ToListAsync();
        return infraFeedbacks.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainFeedback>> FindAsync(Expression<Func<DomainFeedback, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<IEnumerable<DomainFeedback>> GetAllWithUserAsync()
    {
        var infraFeedbacks = await _context.Feedbacks
            .Include(f => f.User)
            .OrderByDescending(f => f.Id)
            .ToListAsync();
            
        return infraFeedbacks.Select(MapToDomain);
    }

    public async Task<(IEnumerable<DomainFeedback> Items, int TotalCount)> GetPagedWithUserAsync(int pageNumber, int pageSize)
    {
        var query = _context.Feedbacks.Include(f => f.User);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(f => f.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items.Select(MapToDomain), totalCount);
    }

    public async Task<DomainFeedback> AddAsync(DomainFeedback entity)
    {
        var infraFeedback = new Feedback
        {
            UserId = entity.UserId,
            Title = entity.Title,
            Content = entity.Content,
            CreatedAt = entity.CreatedAt ?? DateTime.UtcNow
        };

        await _context.Feedbacks.AddAsync(infraFeedback);
        await _context.SaveChangesAsync();
        entity.Id = infraFeedback.Id;
        return entity;
    }

    public async Task UpdateAsync(DomainFeedback entity)
    {
        var infraFeedback = await _context.Feedbacks.FindAsync(entity.Id);
        if (infraFeedback != null)
        {
            infraFeedback.UserId = entity.UserId;
            infraFeedback.Title = entity.Title;
            infraFeedback.Content = entity.Content;
            
            _context.Feedbacks.Update(infraFeedback);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var infraFeedback = await _context.Feedbacks.FindAsync(id);
        if (infraFeedback != null)
        {
            _context.Feedbacks.Remove(infraFeedback);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(DomainFeedback entity)
    {
        await DeleteAsync(entity.Id);
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainFeedback, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.AsQueryable().Any(predicate);
    }

    private static DomainFeedback MapToDomain(Feedback infraFeedback)
    {
        return new DomainFeedback
        {
            Id = infraFeedback.Id,
            UserId = infraFeedback.UserId,
            Title = infraFeedback.Title,
            Content = infraFeedback.Content,
            CreatedAt = infraFeedback.CreatedAt,
            User = infraFeedback.User != null ? new MAEMS.Domain.Entities.User 
            { 
                 UserId = infraFeedback.User.UserId, 
                 Username = infraFeedback.User.Username 
            } : null
        };
    }
}
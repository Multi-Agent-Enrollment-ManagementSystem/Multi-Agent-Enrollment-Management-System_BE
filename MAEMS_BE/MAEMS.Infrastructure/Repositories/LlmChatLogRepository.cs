using System.Linq.Expressions;
using DomainLlmChatLog = MAEMS.Domain.Entities.LlmChatLog;
using InfraLlmChatLog = MAEMS.Infrastructure.Models.LlmChatLog;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace MAEMS.Infrastructure.Repositories;

/// <summary>
/// Legacy interface for backward compatibility with ChatBoxAgent and ChatBoxController
/// Uses Infrastructure Models directly
/// </summary>
public interface ILlmChatLogRepositoryLegacy
{
    Task<InfraLlmChatLog> AddAsync(InfraLlmChatLog chatLog, CancellationToken cancellationToken = default);
    Task<List<InfraLlmChatLog>> GetByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<InfraLlmChatLog?> GetByIdAsync(int chatId, CancellationToken cancellationToken = default);
}

public sealed class LlmChatLogRepository : ILlmChatLogRepository, ILlmChatLogRepositoryLegacy
{
    private readonly postgresContext _context;

    public LlmChatLogRepository(postgresContext context)
    {
        _context = context;
    }

    #region IGenericRepository<LlmChatLog> Implementation

    public async Task<DomainLlmChatLog?> GetByIdAsync(int id)
    {
        var infra = await _context.LlmChatLogs.FindAsync(id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainLlmChatLog>> GetAllAsync()
    {
        var infraList = await Task.FromResult(_context.LlmChatLogs.ToList());
        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainLlmChatLog>> FindAsync(Expression<Func<DomainLlmChatLog, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainLlmChatLog> AddAsync(DomainLlmChatLog entity)
    {
        var infra = MapToInfra(entity);
        _context.LlmChatLogs.Add(infra);
        await _context.SaveChangesAsync();
        entity.ChatId = infra.ChatId;
        return entity;
    }

    public async Task UpdateAsync(DomainLlmChatLog entity)
    {
        var infra = await _context.LlmChatLogs.FindAsync(entity.ChatId);
        if (infra != null)
        {
            infra.UserId = entity.UserId;
            infra.UserQuery = entity.UserQuery;
            infra.Message = entity.Message;
            infra.LlmResponse = entity.LlmResponse;
            infra.CreatedAt = entity.CreatedAt;
            _context.LlmChatLogs.Update(infra);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainLlmChatLog entity)
    {
        var infra = await _context.LlmChatLogs.FindAsync(entity.ChatId);
        if (infra != null)
        {
            _context.LlmChatLogs.Remove(infra);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainLlmChatLog, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    #endregion

    #region ILlmChatLogRepository Implementation - New Paged Query

    public async Task<(IReadOnlyList<DomainLlmChatLog> Items, int TotalCount)> GetLlmChatLogsPagedAsync(
        int? userId = null,
        string? userQuery = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.LlmChatLogs
            .AsNoTracking()
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(userQuery))
            query = query.Where(x => EF.Functions.ILike(x.UserQuery, $"%{userQuery}%"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.UserQuery, $"%{search}%") ||
                EF.Functions.ILike(x.Message, $"%{search}%") ||
                EF.Functions.ILike(x.LlmResponse, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "chatId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "chatid" => sortDesc ? query.OrderByDescending(x => x.ChatId) : query.OrderBy(x => x.ChatId),
            "userid" => sortDesc ? query.OrderByDescending(x => x.UserId) : query.OrderBy(x => x.UserId),
            "userquery" => sortDesc ? query.OrderByDescending(x => x.UserQuery) : query.OrderBy(x => x.UserQuery),
            "message" => sortDesc ? query.OrderByDescending(x => x.Message) : query.OrderBy(x => x.Message),
            "llmresponse" => sortDesc ? query.OrderByDescending(x => x.LlmResponse) : query.OrderBy(x => x.LlmResponse),
            "createdat" => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(x => x.ChatId) : query.OrderBy(x => x.ChatId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    #endregion

    #region ILlmChatLogRepositoryLegacy Implementation - For backward compatibility

    async Task<InfraLlmChatLog> ILlmChatLogRepositoryLegacy.AddAsync(InfraLlmChatLog chatLog, CancellationToken cancellationToken)
    {
        _context.LlmChatLogs.Add(chatLog);
        await _context.SaveChangesAsync(cancellationToken);
        return chatLog;
    }

    async Task<List<InfraLlmChatLog>> ILlmChatLogRepositoryLegacy.GetByUserIdAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        return await _context.LlmChatLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    async Task<int> ILlmChatLogRepositoryLegacy.GetCountByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.LlmChatLogs
            .Where(x => x.UserId == userId)
            .CountAsync(cancellationToken);
    }

    async Task<InfraLlmChatLog?> ILlmChatLogRepositoryLegacy.GetByIdAsync(int chatId, CancellationToken cancellationToken)
    {
        return await _context.LlmChatLogs
            .FirstOrDefaultAsync(x => x.ChatId == chatId, cancellationToken);
    }

    #endregion

    #region Mapping Helpers

    private static DomainLlmChatLog MapToDomain(InfraLlmChatLog infra)
    {
        return new DomainLlmChatLog
        {
            ChatId = infra.ChatId,
            UserId = infra.UserId,
            UserQuery = infra.UserQuery ?? string.Empty,
            Message = infra.Message ?? string.Empty,
            LlmResponse = infra.LlmResponse ?? string.Empty,
            CreatedAt = infra.CreatedAt
        };
    }

    private static InfraLlmChatLog MapToInfra(DomainLlmChatLog domain)
    {
        return new InfraLlmChatLog
        {
            ChatId = domain.ChatId,
            UserId = domain.UserId,
            UserQuery = domain.UserQuery,
            Message = domain.Message,
            LlmResponse = domain.LlmResponse,
            CreatedAt = domain.CreatedAt
        };
    }

    #endregion
}

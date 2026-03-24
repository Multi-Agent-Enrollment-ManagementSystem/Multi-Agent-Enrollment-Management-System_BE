using System.Linq.Expressions;
using DomainAgentLog = MAEMS.Domain.Entities.AgentLog;
using InfraAgentLog = MAEMS.Infrastructure.Models.AgentLog;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace MAEMS.Infrastructure.Repositories;

public sealed class AgentLogRepository : IAgentLogRepository
{
    private readonly postgresContext _context;

    public AgentLogRepository(postgresContext context)
    {
        _context = context;
    }

    public async Task<DomainAgentLog?> GetByIdAsync(int id)
    {
        var infra = await _context.AgentLogs.FindAsync(id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainAgentLog>> GetAllAsync()
    {
        var infraList = await Task.FromResult(_context.AgentLogs.ToList());
        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainAgentLog>> FindAsync(Expression<Func<DomainAgentLog, bool>> predicate)
    {
        // Fallback to in-memory predicate to match the pattern used in other repositories.
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainAgentLog> AddAsync(DomainAgentLog entity)
    {
        var infra = MapToInfra(entity);
        _context.AgentLogs.Add(infra);
        await _context.SaveChangesAsync();
        entity.LogId = infra.LogId;
        return entity;
    }

    public async Task UpdateAsync(DomainAgentLog entity)
    {
        var infra = await _context.AgentLogs.FindAsync(entity.LogId);
        if (infra != null)
        {
            infra.ApplicationId = entity.ApplicationId;
            infra.DocumentId = entity.DocumentId;
            infra.AgentType = entity.AgentType;
            infra.Action = entity.Action;
            infra.Status = entity.Status;
            infra.OutputData = entity.OutputData;
            infra.CreatedAt = entity.CreatedAt;
            _context.AgentLogs.Update(infra);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainAgentLog entity)
    {
        var infra = await _context.AgentLogs.FindAsync(entity.LogId);
        if (infra != null)
        {
            _context.AgentLogs.Remove(infra);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainAgentLog, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    public async Task<(IReadOnlyList<DomainAgentLog> Items, int TotalCount)> GetAgentLogsPagedAsync(
        int? applicationId = null,
        int? documentId = null,
        string? agentType = null,
        string? status = null,
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

        var query = _context.AgentLogs
            .AsNoTracking()
            .AsQueryable();

        if (applicationId.HasValue)
            query = query.Where(a => a.ApplicationId == applicationId.Value);

        if (documentId.HasValue)
            query = query.Where(a => a.DocumentId == documentId.Value);

        if (!string.IsNullOrWhiteSpace(agentType))
            query = query.Where(a => EF.Functions.ILike(a.AgentType, $"%{agentType}%"));

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(a => EF.Functions.ILike(a.Status, $"%{status}%"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a =>
                EF.Functions.ILike(a.AgentType, $"%{search}%") ||
                EF.Functions.ILike(a.Action, $"%{search}%") ||
                EF.Functions.ILike(a.Status, $"%{search}%") ||
                EF.Functions.ILike(a.OutputData, $"%{search}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "logId" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "logid" => sortDesc ? query.OrderByDescending(x => x.LogId) : query.OrderBy(x => x.LogId),
            "applicationid" => sortDesc ? query.OrderByDescending(x => x.ApplicationId) : query.OrderBy(x => x.ApplicationId),
            "documentid" => sortDesc ? query.OrderByDescending(x => x.DocumentId) : query.OrderBy(x => x.DocumentId),
            "agenttype" => sortDesc ? query.OrderByDescending(x => x.AgentType) : query.OrderBy(x => x.AgentType),
            "action" => sortDesc ? query.OrderByDescending(x => x.Action) : query.OrderBy(x => x.Action),
            "status" => sortDesc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "createdat" => sortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            _ => sortDesc ? query.OrderByDescending(x => x.LogId) : query.OrderBy(x => x.LogId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    private static DomainAgentLog MapToDomain(InfraAgentLog infra)
    {
        return new DomainAgentLog
        {
            LogId = infra.LogId,
            ApplicationId = infra.ApplicationId,
            DocumentId = infra.DocumentId,
            AgentType = infra.AgentType ?? string.Empty,
            Action = infra.Action ?? string.Empty,
            Status = infra.Status ?? string.Empty,
            OutputData = infra.OutputData,
            CreatedAt = infra.CreatedAt
        };
    }

    private static InfraAgentLog MapToInfra(DomainAgentLog domain)
    {
        return new InfraAgentLog
        {
            LogId = domain.LogId,
            ApplicationId = domain.ApplicationId,
            DocumentId = domain.DocumentId,
            AgentType = domain.AgentType,
            Action = domain.Action,
            Status = domain.Status,
            OutputData = domain.OutputData,
            CreatedAt = domain.CreatedAt
        };
    }
}

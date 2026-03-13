using System.Linq.Expressions;
using DomainAgentLog = MAEMS.Domain.Entities.AgentLog;
using InfraAgentLog = MAEMS.Infrastructure.Models.AgentLog;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;

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

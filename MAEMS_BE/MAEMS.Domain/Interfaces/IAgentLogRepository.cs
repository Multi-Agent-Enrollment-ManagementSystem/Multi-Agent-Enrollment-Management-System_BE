using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IAgentLogRepository : IGenericRepository<AgentLog>
{
    Task<(IReadOnlyList<AgentLog> Items, int TotalCount)> GetAgentLogsPagedAsync(
        int? applicationId = null,
        int? documentId = null,
        string? agentType = null,
        string? status = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}

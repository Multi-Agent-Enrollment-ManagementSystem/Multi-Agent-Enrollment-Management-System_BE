using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface ILlmChatLogRepository : IGenericRepository<LlmChatLog>
{
    // New paged query for API
    Task<(IReadOnlyList<LlmChatLog> Items, int TotalCount)> GetLlmChatLogsPagedAsync(
        int? userId = null,
        string? userQuery = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}

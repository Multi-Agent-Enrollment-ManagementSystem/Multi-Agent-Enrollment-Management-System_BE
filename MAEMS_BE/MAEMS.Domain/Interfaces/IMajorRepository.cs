using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IMajorRepository : IGenericRepository<Major>
{
    Task<Major?> GetByMajorCodeAsync(string majorCode);
    Task<IEnumerable<Major>> GetActiveMajorsAsync();

    // SQL-level filtering/sorting/paging for majors listing.
    Task<(IReadOnlyList<Major> Items, int TotalCount)> GetMajorsPagedAsync(
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

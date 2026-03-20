using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IProgramRepository : IGenericRepository<Program>
{
    Task<IEnumerable<Program>> GetActiveProgramsAsync();
    Task<IEnumerable<Program>> GetProgramsByMajorIdAsync(int majorId);
    Task<IEnumerable<Program>> GetProgramsByEnrollmentYearIdAsync(int enrollmentYearId);

    // Admin listing: SQL-level filtering/sorting/paging.
    Task<(IReadOnlyList<Program> Items, int TotalCount)> GetProgramsPagedAsync(
        int? majorId,
        int? enrollmentYearId,
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // SQL-level filtering/sorting/paging for basic program listing.
    // Returns: (items, totalCount)
    Task<(IReadOnlyList<Program> Items, int TotalCount)> GetProgramsBasicByFilterPagedAsync(
        int? majorId,
        string? searchName,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

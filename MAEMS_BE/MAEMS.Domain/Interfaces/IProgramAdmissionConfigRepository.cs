using MAEMS.Domain.Entities;
using System.Linq.Expressions;

namespace MAEMS.Domain.Interfaces;

public interface IProgramAdmissionConfigRepository : IGenericRepository<ProgramAdmissionConfig>
{
    Task<IEnumerable<ProgramAdmissionConfig>> GetActiveConfigsAsync();
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByProgramIdAsync(int programId);
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByCampusIdAsync(int campusId);
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByAdmissionTypeIdAsync(int admissionTypeId);

    // SQL-level filtering/sorting/paging for admin listing.
    Task<(IReadOnlyList<ProgramAdmissionConfig> Items, int TotalCount)> GetConfigsPagedAsync(
        int? programId,
        int? campusId,
        int? admissionTypeId,
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

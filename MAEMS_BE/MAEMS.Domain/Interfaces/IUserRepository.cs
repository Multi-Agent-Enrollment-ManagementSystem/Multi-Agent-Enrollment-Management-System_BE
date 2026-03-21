namespace MAEMS.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<Entities.User>
{
    Task<Entities.User?> GetByUsernameAsync(string username);
    Task<Entities.User?> GetByEmailAsync(string email);
    Task<bool> IsUsernameExistsAsync(string username);
    Task<bool> IsEmailExistsAsync(string email);

    // SQL-level filtering/sorting/paging for admin users listing.
    // Returns: (items, totalCount)
    Task<(IReadOnlyList<Entities.User> Items, int TotalCount)> GetUsersPagedAsync(
        int? roleId,
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

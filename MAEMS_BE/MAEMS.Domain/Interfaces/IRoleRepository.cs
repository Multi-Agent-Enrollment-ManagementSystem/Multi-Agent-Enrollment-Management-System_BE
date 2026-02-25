using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<Role?> GetByNameAsync(string name);
    Task<IEnumerable<Role>> GetActiveRolesAsync();
}

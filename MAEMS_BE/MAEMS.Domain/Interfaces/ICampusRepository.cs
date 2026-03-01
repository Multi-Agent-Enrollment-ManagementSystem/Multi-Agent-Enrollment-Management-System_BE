using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface ICampusRepository : IGenericRepository<Campus>
{
    Task<IEnumerable<Campus>> GetActiveCampusesAsync();
}

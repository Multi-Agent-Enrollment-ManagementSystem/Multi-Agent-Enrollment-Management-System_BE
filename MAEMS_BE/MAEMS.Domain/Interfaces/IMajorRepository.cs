using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IMajorRepository : IGenericRepository<Major>
{
    Task<Major?> GetByMajorCodeAsync(string majorCode);
    Task<IEnumerable<Major>> GetActiveMajorsAsync();
}

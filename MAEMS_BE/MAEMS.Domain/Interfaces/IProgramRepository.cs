using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IProgramRepository : IGenericRepository<Program>
{
    Task<IEnumerable<Program>> GetActiveProgramsAsync();
    Task<IEnumerable<Program>> GetProgramsByMajorIdAsync(int majorId);
}

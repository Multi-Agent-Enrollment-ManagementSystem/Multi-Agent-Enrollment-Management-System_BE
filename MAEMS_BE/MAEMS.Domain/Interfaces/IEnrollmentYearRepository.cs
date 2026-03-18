using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IEnrollmentYearRepository : IGenericRepository<EnrollmentYear>
{
    Task<IEnumerable<EnrollmentYear>> GetAllAsync();
    Task<EnrollmentYear?> GetByIdAsync(int id);
}

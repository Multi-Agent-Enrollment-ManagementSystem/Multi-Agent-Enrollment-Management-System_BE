namespace MAEMS.Domain.Interfaces;

public interface IApplicantRepository : IGenericRepository<Entities.Applicant>
{
    Task<Entities.Applicant?> GetByUserIdAsync(int userId);
}

namespace MAEMS.Domain.Interfaces;

public interface IApplicationRepository : IGenericRepository<Entities.Application>
{
    Task<Entities.Application?> GetByApplicantIdAsync(int applicantId);
    Task<IEnumerable<Entities.Application>> GetByStatusAsync(string status);
    Task<IEnumerable<Entities.Application>> GetByProgramIdAsync(int programId);
    Task<IEnumerable<Entities.Application>> GetByEnrollmentYearIdAsync(int enrollmentYearId);
    Task<IEnumerable<Entities.Application>> GetByAssignedOfficerIdAsync(int officerId);
    Task<IEnumerable<Entities.Application>> GetApplicationsRequiringReviewAsync();
    Task<IEnumerable<Entities.Application>> GetApplicationsWithDetailsAsync();
}
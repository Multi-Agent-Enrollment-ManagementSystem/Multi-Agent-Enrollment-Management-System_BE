namespace MAEMS.Domain.Interfaces;

public interface IApplicationRepository : IGenericRepository<Entities.Application>
{
    Task<IEnumerable<Entities.Application>> GetAllByApplicantIdAsync(int applicantId);
    Task<Entities.Application?> GetByApplicantIdAsync(int applicantId);
    Task<IEnumerable<Entities.Application>> GetByStatusAsync(string status);
    Task<IEnumerable<Entities.Application>> GetByProgramIdAsync(int programId);
    Task<IEnumerable<Entities.Application>> GetByEnrollmentYearIdAsync(int enrollmentYearId);
    Task<IEnumerable<Entities.Application>> GetByAssignedOfficerIdAsync(int officerId);
    Task<IEnumerable<Entities.Application>> GetApplicationsRequiringReviewAsync();
    Task<IEnumerable<Entities.Application>> GetApplicationsWithDetailsAsync();
    Task<(IReadOnlyList<Entities.Application> Items, int TotalCount)> GetApplicationsPagedAsync(
        int? programId,
        int? campusId,
        int? admissionTypeId,
        string? status,
        bool? requiresReview,
        int? assignedOfficerId,
        string? search,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
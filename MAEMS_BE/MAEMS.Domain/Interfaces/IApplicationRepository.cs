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

    // Reporting: count applications (Status != draft) grouped by week start (based on SubmittedAt)
    Task<IReadOnlyList<(DateTime WeekStart, int Count)>> CountNonDraftByWeekAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default);

    // Reporting: count applications grouped by campus
    Task<IReadOnlyList<(int CampusId, string? CampusName, int Count)>> CountByCampusAsync(
        CancellationToken cancellationToken = default);

    // Reporting: count non-draft applications grouped by program for a specific campus
    Task<IReadOnlyList<(int ProgramId, string? ProgramName, int Count)>> CountNonDraftByProgramInCampusAsync(
        int campusId,
        CancellationToken cancellationToken = default);

    // Reporting: counts by status (approved/rejected/pending)
    Task<(int NumApproved, int NumRejected, int NumPending)> CountApprovedRejectedPendingAsync(
        CancellationToken cancellationToken = default);

    // Reporting: count applications grouped by assigned officer
    Task<IReadOnlyList<(int? AssignedOfficerId, string? AssignedOfficerName, int Count)>> CountByAssignedOfficerAsync(
        CancellationToken cancellationToken = default);
}
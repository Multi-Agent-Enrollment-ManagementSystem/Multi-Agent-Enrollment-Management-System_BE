using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainApplication = MAEMS.Domain.Entities.Application;
using InfraApplication = MAEMS.Infrastructure.Models.Application;

namespace MAEMS.Infrastructure.Repositories;

public class ApplicationRepository : BaseRepository, IApplicationRepository
{
    public ApplicationRepository(postgresContext context) : base(context) { }

    public async Task<DomainApplication?> GetByIdAsync(int id)
    {
        var infraApplication = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        return infraApplication == null ? null : MapToDomain(infraApplication);
    }

    public async Task<IEnumerable<DomainApplication>> GetAllAsync()
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<DomainApplication?> GetByApplicantIdAsync(int applicantId)
    {
        var infraApplication = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .FirstOrDefaultAsync(a => a.ApplicantId == applicantId);

        return infraApplication == null ? null : MapToDomain(infraApplication);
    }

    public async Task<IEnumerable<DomainApplication>> GetAllByApplicantIdAsync(int applicantId)
    {
        var infraApps = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.ApplicantId == applicantId)
            .ToListAsync();

        return infraApps.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByStatusAsync(string status)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.Status == status)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByProgramIdAsync(int programId)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.Config.ProgramId == programId)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByEnrollmentYearIdAsync(int enrollmentYearId)
    {
        // EnrollmentYear không có trong DB hiện tại
        return new List<DomainApplication>();
    }

    public async Task<IEnumerable<DomainApplication>> GetByAssignedOfficerIdAsync(int officerId)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.AssignedOfficerId == officerId)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetApplicationsRequiringReviewAsync()
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Config)
                .ThenInclude(c => c.Program)
            .Include(a => a.Config)
                .ThenInclude(c => c.Campus)
            .Include(a => a.Config)
                .ThenInclude(c => c.AdmissionType)
                    .ThenInclude(at => at.EnrollmentYear)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.RequiresReview == true)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetApplicationsWithDetailsAsync()
    {
        return await GetAllAsync();
    }

    public async Task<IEnumerable<DomainApplication>> FindAsync(Expression<Func<DomainApplication, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainApplication> AddAsync(DomainApplication entity)
    {
        // Validate config tồn tại
        var configExists = await _context.ProgramAdmissionConfigs
            .AnyAsync(c => c.ConfigId == entity.ConfigId && c.IsActive == true);

        if (!configExists)
        {
            throw new InvalidOperationException("No active config found for the provided ConfigId");
        }

        var infraApplication = new InfraApplication
        {
            ApplicantId = entity.ApplicantId,
            ConfigId = entity.ConfigId,
            Status = entity.Status ?? "draft",
            SubmittedAt = entity.SubmittedAt,
            LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            AssignedOfficerId = entity.AssignedOfficerId,
            Notes = entity.Notes,
            RequiresReview = entity.RequiresReview ?? false
        };

        await _context.Applications.AddAsync(infraApplication);
        entity.ApplicationId = infraApplication.ApplicationId;
        return entity;
    }

    public async Task UpdateAsync(DomainApplication entity)
    {
        var infraApplication = await _context.Applications.FindAsync(entity.ApplicationId);
        if (infraApplication != null)
        {
            infraApplication.Status = entity.Status;
            infraApplication.LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            infraApplication.AssignedOfficerId = entity.AssignedOfficerId;
            infraApplication.Notes = entity.Notes;
            infraApplication.RequiresReview = entity.RequiresReview;

            _context.Applications.Update(infraApplication);
        }
    }

    public async Task DeleteAsync(DomainApplication entity)
    {
        var infraApplication = await _context.Applications.FindAsync(entity.ApplicationId);
        if (infraApplication != null)
        {
            _context.Applications.Remove(infraApplication);
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainApplication, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    private static DomainApplication MapToDomain(InfraApplication infraApplication)
    {
        return new DomainApplication
        {
            ApplicationId = infraApplication.ApplicationId,
            ApplicantId = infraApplication.ApplicantId,
            ConfigId = infraApplication.ConfigId,
            ProgramId = infraApplication.Config?.ProgramId,
            CampusId = infraApplication.Config?.CampusId,
            AdmissionTypeId = infraApplication.Config?.AdmissionTypeId,
            Status = infraApplication.Status,
            SubmittedAt = infraApplication.SubmittedAt,
            LastUpdated = infraApplication.LastUpdated,
            AssignedOfficerId = infraApplication.AssignedOfficerId,
            Notes = infraApplication.Notes,
            RequiresReview = infraApplication.RequiresReview,
            // Navigation properties
            ApplicantName = infraApplication.Applicant?.FullName,
            ProgramName = infraApplication.Config?.Program?.ProgramName,
            CampusName = infraApplication.Config?.Campus?.Name,
            AdmissionTypeName = infraApplication.Config?.AdmissionType?.AdmissionTypeName,
            AssignedOfficerName = infraApplication.AssignedOfficer?.Username,
            EnrollmentYear = infraApplication.Config?.AdmissionType?.EnrollmentYear?.Year
        };
    }
}
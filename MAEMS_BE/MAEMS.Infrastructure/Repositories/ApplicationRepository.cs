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
    private readonly postgresContext _context;

    public ApplicationRepository(postgresContext context) : base(context)
    {
        _context = context;
    }

    public async Task<DomainApplication?> GetByIdAsync(int id)
    {
        var infraApplication = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        if (infraApplication == null)
            return null;

        return MapToDomain(infraApplication);
    }

    public async Task<DomainApplication?> GetByApplicantIdAsync(int applicantId)
    {
        var infraApplication = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .FirstOrDefaultAsync(a => a.ApplicantId == applicantId);

        if (infraApplication == null)
            return null;

        return MapToDomain(infraApplication);
    }

    public async Task<IEnumerable<DomainApplication>> GetAllAsync()
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByStatusAsync(string status)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.Status == status)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByProgramIdAsync(int programId)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.ProgramId == programId)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByEnrollmentYearIdAsync(int enrollmentYearId)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.EnrollmentYearId == enrollmentYearId)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetByAssignedOfficerIdAsync(int officerId)
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .Where(a => a.AssignedOfficerId == officerId)
            .ToListAsync();

        return infraApplications.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplication>> GetApplicationsRequiringReviewAsync()
    {
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
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
        var infraApplications = await _context.Applications
            .Include(a => a.Applicant)
            .Include(a => a.Program)
            .Include(a => a.EnrollmentYear)
            .Include(a => a.Campus)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .ToListAsync();

        var domainApplications = infraApplications.Select(MapToDomain);
        return domainApplications.Where(predicate.Compile());
    }

    public async Task<DomainApplication> AddAsync(DomainApplication entity)
    {
        var infraApplication = new InfraApplication
        {
            ApplicantId = entity.ApplicantId,
            ProgramId = entity.ProgramId,
            EnrollmentYearId = entity.EnrollmentYearId,
            CampusId = entity.CampusId,
            AdmissionTypeId = entity.AdmissionTypeId,
            Status = entity.Status,
            SubmittedAt = entity.SubmittedAt ?? DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
            AssignedOfficerId = entity.AssignedOfficerId,
            Notes = entity.Notes,
            RequiresReview = entity.RequiresReview ?? false
        };

        await _context.Applications.AddAsync(infraApplication);

        entity.ApplicationId = infraApplication.ApplicationId;
        entity.SubmittedAt = infraApplication.SubmittedAt;
        entity.LastUpdated = infraApplication.LastUpdated;
        return entity;
    }

    public async Task UpdateAsync(DomainApplication entity)
    {
        var infraApplication = await _context.Applications.FindAsync(entity.ApplicationId);
        if (infraApplication != null)
        {
            infraApplication.ApplicantId = entity.ApplicantId;
            infraApplication.ProgramId = entity.ProgramId;
            infraApplication.EnrollmentYearId = entity.EnrollmentYearId;
            infraApplication.CampusId = entity.CampusId;
            infraApplication.AdmissionTypeId = entity.AdmissionTypeId;
            infraApplication.Status = entity.Status;
            infraApplication.LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            infraApplication.AssignedOfficerId = entity.AssignedOfficerId;
            infraApplication.Notes = entity.Notes;
            infraApplication.RequiresReview = entity.RequiresReview;

            _context.Applications.Update(infraApplication);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainApplication entity)
    {
        var infraApplication = await _context.Applications.FindAsync(entity.ApplicationId);
        if (infraApplication != null)
        {
            _context.Applications.Remove(infraApplication);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainApplication, bool>> predicate)
    {
        var infraApplications = await _context.Applications.ToListAsync();
        var domainApplications = infraApplications.Select(MapToDomainBasic);
        return domainApplications.Any(predicate.Compile());
    }

    public async Task<IEnumerable<MAEMS.Domain.Entities.Application>> GetAllByApplicantIdAsync(int applicantId)
    {
        var infraApps = await _context.Applications
            .Include(a => a.Program)
            .Include(a => a.Campus)
            .Include(a => a.Applicant)
            .Include(a => a.AdmissionType)
            .Include(a => a.AssignedOfficer)
            .Include(a => a.EnrollmentYear)
            .Where(a => a.ApplicantId == applicantId)
            .ToListAsync();

        return infraApps.Select(a => new MAEMS.Domain.Entities.Application
        {
            ApplicationId = a.ApplicationId,
            ApplicantId = a.ApplicantId,
            ProgramId = a.ProgramId,
            EnrollmentYearId = a.EnrollmentYearId,
            CampusId = a.CampusId,
            AdmissionTypeId = a.AdmissionTypeId,
            Status = a.Status,
            SubmittedAt = a.SubmittedAt,
            LastUpdated = a.LastUpdated,
            AssignedOfficerId = a.AssignedOfficerId,
            Notes = a.Notes,
            RequiresReview = a.RequiresReview,
            // Lấy tên từ navigation property
            ProgramName = a.Program?.ProgramName,
            CampusName = a.Campus?.Name,
            ApplicantName = a.Applicant?.FullName,
            AdmissionTypeName = a.AdmissionType?.AdmissionTypeName,
            AssignedOfficerName = a.AssignedOfficer?.Username,
            EnrollmentYear = a.EnrollmentYear?.Year // hoặc .Year.ToString() nếu có
        }).ToList();
    }
    private static DomainApplication MapToDomain(InfraApplication infraApplication)
    {
        return new DomainApplication
        {
            ApplicationId = infraApplication.ApplicationId,
            ApplicantId = infraApplication.ApplicantId,
            ProgramId = infraApplication.ProgramId,
            EnrollmentYearId = infraApplication.EnrollmentYearId,
            CampusId = infraApplication.CampusId,
            AdmissionTypeId = infraApplication.AdmissionTypeId,
            Status = infraApplication.Status,
            SubmittedAt = infraApplication.SubmittedAt,
            LastUpdated = infraApplication.LastUpdated,
            AssignedOfficerId = infraApplication.AssignedOfficerId,
            Notes = infraApplication.Notes,
            RequiresReview = infraApplication.RequiresReview,
            // Navigation properties - sửa để sử dụng field Name thay vì CampusName
            ApplicantName = infraApplication.Applicant?.FullName,
            ProgramName = infraApplication.Program?.ProgramName,
            EnrollmentYear = infraApplication.EnrollmentYear?.Year,
            CampusName = infraApplication.Campus?.Name, // Sử dụng Name thay vì CampusName
            AdmissionTypeName = infraApplication.AdmissionType?.AdmissionTypeName,
            AssignedOfficerName = infraApplication.AssignedOfficer?.Username // Sử dụng Username thay vì FullName
        };
    }

    private static DomainApplication MapToDomainBasic(InfraApplication infraApplication)
    {
        return new DomainApplication
        {
            ApplicationId = infraApplication.ApplicationId,
            ApplicantId = infraApplication.ApplicantId,
            ProgramId = infraApplication.ProgramId,
            EnrollmentYearId = infraApplication.EnrollmentYearId,
            CampusId = infraApplication.CampusId,
            AdmissionTypeId = infraApplication.AdmissionTypeId,
            Status = infraApplication.Status,
            SubmittedAt = infraApplication.SubmittedAt,
            LastUpdated = infraApplication.LastUpdated,
            AssignedOfficerId = infraApplication.AssignedOfficerId,
            Notes = infraApplication.Notes,
            RequiresReview = infraApplication.RequiresReview
        };
    }
}
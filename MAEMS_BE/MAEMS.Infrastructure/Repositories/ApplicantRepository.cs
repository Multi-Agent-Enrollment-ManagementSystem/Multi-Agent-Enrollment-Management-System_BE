using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainApplicant = MAEMS.Domain.Entities.Applicant;
using InfraApplicant = MAEMS.Infrastructure.Models.Applicant;

namespace MAEMS.Infrastructure.Repositories;

public class ApplicantRepository : BaseRepository, IApplicantRepository
{
    public ApplicantRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainApplicant?> GetByUserIdAsync(int userId)
    {
        var infraApplicant = await _context.Applicants
            .FirstOrDefaultAsync(a => a.UserId == userId);
        
        if (infraApplicant == null)
            return null;

        return MapToDomain(infraApplicant);
    }

    public async Task<DomainApplicant?> GetByIdAsync(int id)
    {
        var infraApplicant = await _context.Applicants.FindAsync(id);
        
        if (infraApplicant == null)
            return null;

        return MapToDomain(infraApplicant);
    }

    public async Task<IEnumerable<DomainApplicant>> GetAllAsync()
    {
        var infraApplicants = await _context.Applicants.ToListAsync();
        return infraApplicants.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainApplicant>> FindAsync(Expression<Func<DomainApplicant, bool>> predicate)
    {
        var infraApplicants = await _context.Applicants.ToListAsync();
        var domainApplicants = infraApplicants.Select(MapToDomain);
        return domainApplicants.Where(predicate.Compile());
    }

    public async Task<DomainApplicant> AddAsync(DomainApplicant entity)
    {
        var infraApplicant = new InfraApplicant
        {
            UserId = entity.UserId,
            FullName = entity.FullName,
            DateOfBirth = entity.DateOfBirth,
            Gender = entity.Gender,
            HighSchoolName = entity.HighSchoolName,
            HighSchoolDistrict = entity.HighSchoolDistrict,
            HighSchoolProvince = entity.HighSchoolProvince,
            GraduationYear = entity.GraduationYear,
            IdIssueNumber = entity.IdIssueNumber,
            IdIssueDate = entity.IdIssueDate,
            IdIssuePlace = entity.IdIssuePlace,
            ContactName = entity.ContactName,
            ContactAddress = entity.ContactAddress,
            ContactPhone = entity.ContactPhone,
            ContactEmail = entity.ContactEmail,
            AllowShare = entity.AllowShare,
            CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
        };

        await _context.Applicants.AddAsync(infraApplicant);
        
        entity.ApplicantId = infraApplicant.ApplicantId;
        entity.CreatedAt = infraApplicant.CreatedAt;
        return entity;
    }

    public async Task UpdateAsync(DomainApplicant entity)
    {
        var infraApplicant = await _context.Applicants.FindAsync(entity.ApplicantId);
        if (infraApplicant != null)
        {
            infraApplicant.FullName = entity.FullName;
            infraApplicant.DateOfBirth = entity.DateOfBirth;
            infraApplicant.Gender = entity.Gender;
            infraApplicant.HighSchoolName = entity.HighSchoolName;
            infraApplicant.HighSchoolDistrict = entity.HighSchoolDistrict;
            infraApplicant.HighSchoolProvince = entity.HighSchoolProvince;
            infraApplicant.GraduationYear = entity.GraduationYear;
            infraApplicant.IdIssueNumber = entity.IdIssueNumber;
            infraApplicant.IdIssueDate = entity.IdIssueDate;
            infraApplicant.IdIssuePlace = entity.IdIssuePlace;
            infraApplicant.ContactName = entity.ContactName;
            infraApplicant.ContactAddress = entity.ContactAddress;
            infraApplicant.ContactPhone = entity.ContactPhone;
            infraApplicant.ContactEmail = entity.ContactEmail;
            infraApplicant.AllowShare = entity.AllowShare;

            _context.Applicants.Update(infraApplicant);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainApplicant entity)
    {
        var infraApplicant = await _context.Applicants.FindAsync(entity.ApplicantId);
        if (infraApplicant != null)
        {
            _context.Applicants.Remove(infraApplicant);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainApplicant, bool>> predicate)
    {
        var infraApplicants = await _context.Applicants.ToListAsync();
        var domainApplicants = infraApplicants.Select(MapToDomain);
        return domainApplicants.Any(predicate.Compile());
    }

    private static DomainApplicant MapToDomain(InfraApplicant infraApplicant)
    {
        return new DomainApplicant
        {
            ApplicantId = infraApplicant.ApplicantId,
            UserId = infraApplicant.UserId,
            FullName = infraApplicant.FullName ?? string.Empty,
            DateOfBirth = infraApplicant.DateOfBirth,
            Gender = infraApplicant.Gender,
            HighSchoolName = infraApplicant.HighSchoolName,
            HighSchoolDistrict = infraApplicant.HighSchoolDistrict,
            HighSchoolProvince = infraApplicant.HighSchoolProvince,
            GraduationYear = infraApplicant.GraduationYear,
            IdIssueNumber = infraApplicant.IdIssueNumber,
            IdIssueDate = infraApplicant.IdIssueDate,
            IdIssuePlace = infraApplicant.IdIssuePlace,
            ContactName = infraApplicant.ContactName,
            ContactAddress = infraApplicant.ContactAddress,
            ContactPhone = infraApplicant.ContactPhone,
            ContactEmail = infraApplicant.ContactEmail,
            AllowShare = infraApplicant.AllowShare,
            CreatedAt = infraApplicant.CreatedAt
        };
    }
}

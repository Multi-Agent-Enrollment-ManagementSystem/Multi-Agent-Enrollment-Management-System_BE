using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainAdmissionType = MAEMS.Domain.Entities.AdmissionType;
using InfraAdmissionType = MAEMS.Infrastructure.Models.AdmissionType;

namespace MAEMS.Infrastructure.Repositories;

public class AdmissionTypeRepository : BaseRepository, IAdmissionTypeRepository
{
    public AdmissionTypeRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainAdmissionType?> GetByIdAsync(int id)
    {
        var infraAdmissionType = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .FirstOrDefaultAsync(at => at.AdmissionTypeId == id);
        
        if (infraAdmissionType == null)
            return null;

        return MapToDomain(infraAdmissionType);
    }

    public async Task<IEnumerable<DomainAdmissionType>> GetAllAsync()
    {
        var infraAdmissionTypes = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .ToListAsync();
        return infraAdmissionTypes.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainAdmissionType>> GetActiveAdmissionTypesAsync()
    {
        var infraAdmissionTypes = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .Where(at => at.IsActive == true)
            .ToListAsync();

        return infraAdmissionTypes.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainAdmissionType>> GetAdmissionTypesByEnrollmentYearIdAsync(int enrollmentYearId)
    {
        var infraAdmissionTypes = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .Where(at => at.EnrollmentYearId == enrollmentYearId)
            .ToListAsync();

        return infraAdmissionTypes.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainAdmissionType>> FindAsync(Expression<Func<DomainAdmissionType, bool>> predicate)
    {
        var infraAdmissionTypes = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .ToListAsync();
        var domainAdmissionTypes = infraAdmissionTypes.Select(MapToDomain);
        return domainAdmissionTypes.Where(predicate.Compile());
    }

    public async Task<DomainAdmissionType> AddAsync(DomainAdmissionType entity)
    {
        var infraAdmissionType = new InfraAdmissionType
        {
            AdmissionTypeName = entity.AdmissionTypeName,
            EnrollmentYearId = entity.EnrollmentYearId,
            Type = entity.Type,
            RequiredDocumentList = entity.RequiredDocumentList,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        await _context.AdmissionTypes.AddAsync(infraAdmissionType);
        
        entity.AdmissionTypeId = infraAdmissionType.AdmissionTypeId;
        return entity;
    }

    public async Task UpdateAsync(DomainAdmissionType entity)
    {
        var infraAdmissionType = await _context.AdmissionTypes.FindAsync(entity.AdmissionTypeId);
        if (infraAdmissionType != null)
        {
            infraAdmissionType.AdmissionTypeName = entity.AdmissionTypeName;
            infraAdmissionType.EnrollmentYearId = entity.EnrollmentYearId;
            infraAdmissionType.Type = entity.Type;
            infraAdmissionType.RequiredDocumentList = entity.RequiredDocumentList;
            infraAdmissionType.IsActive = entity.IsActive;

            _context.AdmissionTypes.Update(infraAdmissionType);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainAdmissionType entity)
    {
        var infraAdmissionType = await _context.AdmissionTypes.FindAsync(entity.AdmissionTypeId);
        if (infraAdmissionType != null)
        {
            _context.AdmissionTypes.Remove(infraAdmissionType);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainAdmissionType, bool>> predicate)
    {
        var infraAdmissionTypes = await _context.AdmissionTypes
            .Include(at => at.EnrollmentYear)
            .ToListAsync();
        var domainAdmissionTypes = infraAdmissionTypes.Select(MapToDomain);
        return domainAdmissionTypes.Any(predicate.Compile());
    }

    private static DomainAdmissionType MapToDomain(InfraAdmissionType infraAdmissionType)
    {
        return new DomainAdmissionType
        {
            AdmissionTypeId = infraAdmissionType.AdmissionTypeId,
            AdmissionTypeName = infraAdmissionType.AdmissionTypeName ?? string.Empty,
            EnrollmentYearId = infraAdmissionType.EnrollmentYearId,
            Type = infraAdmissionType.Type ?? string.Empty,
            RequiredDocumentList = infraAdmissionType.RequiredDocumentList,
            IsActive = infraAdmissionType.IsActive,
            CreatedAt = infraAdmissionType.CreatedAt,
            EnrollmentYear = infraAdmissionType.EnrollmentYear?.Year
        };
    }
}

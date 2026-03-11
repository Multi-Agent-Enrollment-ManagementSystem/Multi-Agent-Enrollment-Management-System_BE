using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainProgram = MAEMS.Domain.Entities.Program;
using InfraProgram = MAEMS.Infrastructure.Models.Program;

namespace MAEMS.Infrastructure.Repositories;

public class ProgramRepository : BaseRepository, IProgramRepository
{
    public ProgramRepository(postgresContext context) : base(context)
    {
    }

    public async Task<DomainProgram?> GetByIdAsync(int id)
    {
        var infraProgram = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .FirstOrDefaultAsync(p => p.ProgramId == id);

        if (infraProgram == null)
            return null;

        return MapToDomain(infraProgram);
    }

    public async Task<IEnumerable<DomainProgram>> GetAllAsync()
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .ToListAsync();
        return infraPrograms.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainProgram>> GetActiveProgramsAsync()
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .Where(p => p.IsActive == true)
            .ToListAsync();

        return infraPrograms.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainProgram>> GetProgramsByMajorIdAsync(int majorId)
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .Where(p => p.MajorId == majorId)
            .ToListAsync();

        return infraPrograms.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainProgram>> GetProgramsByEnrollmentYearIdAsync(int enrollmentYearId)
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .Where(p => p.EnrollmentYearId == enrollmentYearId)
            .ToListAsync();

        return infraPrograms.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainProgram>> FindAsync(Expression<Func<DomainProgram, bool>> predicate)
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .ToListAsync();
        var domainPrograms = infraPrograms.Select(MapToDomain);
        return domainPrograms.Where(predicate.Compile());
    }

    public async Task<DomainProgram> AddAsync(DomainProgram entity)
    {
        var infraProgram = new InfraProgram
        {
            ProgramName = entity.ProgramName,
            MajorId = entity.MajorId,
            EnrollmentYearId = entity.EnrollmentYearId,
            Description = entity.Description,
            CareerProspects = entity.CareerProspects,
            Duration = entity.Duration,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };

        await _context.Programs.AddAsync(infraProgram);

        entity.ProgramId = infraProgram.ProgramId;
        return entity;
    }

    public async Task UpdateAsync(DomainProgram entity)
    {
        var infraProgram = await _context.Programs.FindAsync(entity.ProgramId);
        if (infraProgram != null)
        {
            infraProgram.ProgramName = entity.ProgramName;
            infraProgram.MajorId = entity.MajorId;
            infraProgram.EnrollmentYearId = entity.EnrollmentYearId;
            infraProgram.Description = entity.Description;
            infraProgram.CareerProspects = entity.CareerProspects;
            infraProgram.Duration = entity.Duration;
            infraProgram.IsActive = entity.IsActive;

            _context.Programs.Update(infraProgram);
        }
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainProgram entity)
    {
        var infraProgram = await _context.Programs.FindAsync(entity.ProgramId);
        if (infraProgram != null)
        {
            _context.Programs.Remove(infraProgram);
        }
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainProgram, bool>> predicate)
    {
        var infraPrograms = await _context.Programs
            .Include(p => p.Major)
            .Include(p => p.EnrollmentYear)
            .ToListAsync();
        var domainPrograms = infraPrograms.Select(MapToDomain);
        return domainPrograms.Any(predicate.Compile());
    }

    private static DomainProgram MapToDomain(InfraProgram infraProgram)
    {
        return new DomainProgram
        {
            ProgramId = infraProgram.ProgramId,
            ProgramName = infraProgram.ProgramName ?? string.Empty,
            MajorId = infraProgram.MajorId,
            EnrollmentYearId = infraProgram.EnrollmentYearId,
            EnrollmentYear = infraProgram.EnrollmentYear?.Year,
            Description = infraProgram.Description,
            CareerProspects = infraProgram.CareerProspects,
            Duration = infraProgram.Duration,
            IsActive = infraProgram.IsActive,
            CreatedAt = infraProgram.CreatedAt
        };
    }
}

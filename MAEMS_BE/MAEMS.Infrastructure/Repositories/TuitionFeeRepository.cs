using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainTuitionFee = MAEMS.Domain.Entities.TuitionFee;
using InfraTuitionFee = MAEMS.Infrastructure.Models.TuitionFee;

namespace MAEMS.Infrastructure.Repositories;

public class TuitionFeeRepository : BaseRepository, ITuitionFeeRepository
{
    public TuitionFeeRepository(postgresContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetByMajorIdAsync(int majorId)
    {
        var infraFees = await _context.TuitionFees
            .Where(tf => tf.MajorId == majorId && tf.IsActive == true)
            .ToListAsync();

        return infraFees.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetByCampusIdAsync(int campusId)
    {
        var infraFees = await _context.TuitionFees
            .Where(tf => tf.CampusId == campusId && tf.IsActive == true)
            .ToListAsync();

        return infraFees.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetByEnrollmentYearIdAsync(int enrollmentYearId)
    {
        var infraFees = await _context.TuitionFees
            .Where(tf => tf.EnrollmentYearId == enrollmentYearId && tf.IsActive == true)
            .ToListAsync();

        return infraFees.Select(MapToDomain);
    }

    public async Task<DomainTuitionFee?> GetByMajorCampusRegionAsync(int majorId, int campusId, string region, int? enrollmentYearId = null)
    {
        var query = _context.TuitionFees
            .Where(tf => tf.MajorId == majorId 
                && tf.CampusId == campusId 
                && tf.Region == region 
                && tf.IsActive == true
                && tf.FeeType == "REGULAR");

        if (enrollmentYearId.HasValue)
        {
            query = query.Where(tf => tf.EnrollmentYearId == enrollmentYearId);
        }

        var infraFee = await query.FirstOrDefaultAsync();

        return infraFee != null ? MapToDomain(infraFee) : null;
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetActiveFeesAsync()
    {
        var infraFees = await _context.TuitionFees
            .Where(tf => tf.IsActive == true)
            .OrderBy(tf => tf.MajorName)
            .ThenBy(tf => tf.CampusName)
            .ThenBy(tf => tf.Region)
            .ToListAsync();

        return infraFees.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetByFeeTypeAsync(string feeType)
    {
        var infraFees = await _context.TuitionFees
            .Where(tf => tf.FeeType == feeType && tf.IsActive == true)
            .ToListAsync();

        return infraFees.Select(MapToDomain);
    }

    /// <summary>
    /// Calculate tuition fee for a specific semester based on business rules:
    /// - HK1-3: Base amount
    /// - HK4-6: Base amount + 6.3%
    /// - HK7+: (Base amount + 6.3%) + 6.5%
    /// </summary>
    public decimal CalculateSemesterFee(DomainTuitionFee baseFee, int semesterNumber)
    {
        if (semesterNumber < 1)
            throw new ArgumentException("Semester number must be greater than 0", nameof(semesterNumber));

        decimal amount = baseFee.BaseAmount;

        // Apply campus discount
        if (baseFee.CampusDiscountPercent.HasValue && baseFee.CampusDiscountPercent.Value > 0)
        {
            amount = amount * (1 - baseFee.CampusDiscountPercent.Value / 100);
        }

        // Apply semester increase rules
        if (semesterNumber >= 1 && semesterNumber <= 3)
        {
            // HK1-3: No increase
            return amount;
        }
        else if (semesterNumber >= 4 && semesterNumber <= 6)
        {
            // HK4-6: Increase 6.3%
            return amount * 1.063m;
        }
        else // semesterNumber >= 7
        {
            // HK7+: First apply 6.3%, then apply 6.5% on the increased amount
            decimal hk4Amount = amount * 1.063m;
            return hk4Amount * 1.065m;
        }
    }

    public async Task<DomainTuitionFee?> GetByIdAsync(int id)
    {
        var infraFee = await _context.TuitionFees.FindAsync(id);
        return infraFee != null ? MapToDomain(infraFee) : null;
    }

    public async Task<IEnumerable<DomainTuitionFee>> GetAllAsync()
    {
        var infraFees = await _context.TuitionFees.ToListAsync();
        return infraFees.Select(MapToDomain);
    }

    public async Task<DomainTuitionFee> AddAsync(DomainTuitionFee entity)
    {
        var infraFee = MapToInfra(entity);
        infraFee.CreatedAt = DateTime.UtcNow;

        await _context.TuitionFees.AddAsync(infraFee);
        await _context.SaveChangesAsync();

        return MapToDomain(infraFee);
    }

    public async Task UpdateAsync(DomainTuitionFee entity)
    {
        var infraFee = await _context.TuitionFees.FindAsync(entity.TuitionFeeId);

        if (infraFee == null)
            throw new InvalidOperationException($"TuitionFee with ID {entity.TuitionFeeId} not found");

        // Update properties
        infraFee.MajorId = entity.MajorId;
        infraFee.MajorName = entity.MajorName;
        infraFee.CampusId = entity.CampusId;
        infraFee.CampusName = entity.CampusName;
        infraFee.EnrollmentYearId = entity.EnrollmentYearId;
        infraFee.EnrollmentYearName = entity.EnrollmentYear;
        infraFee.Region = entity.Region;
        infraFee.FeeType = entity.FeeType;
        infraFee.BaseAmount = entity.BaseAmount;
        infraFee.CampusDiscountPercent = entity.CampusDiscountPercent;
        infraFee.SemesterIncreaseRules = entity.SemesterIncreaseRules;
        infraFee.Currency = entity.Currency;
        infraFee.Description = entity.Description;
        infraFee.Notes = entity.Notes;
        infraFee.EffectiveFrom = entity.EffectiveFrom;
        infraFee.EffectiveTo = entity.EffectiveTo;
        infraFee.IsActive = entity.IsActive;
        infraFee.UpdatedAt = DateTime.UtcNow;
    }

    public async Task DeleteAsync(DomainTuitionFee entity)
    {
        var infraFee = await _context.TuitionFees.FindAsync(entity.TuitionFeeId);

        if (infraFee == null)
            throw new InvalidOperationException($"TuitionFee with ID {entity.TuitionFeeId} not found");

        _context.TuitionFees.Remove(infraFee);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainTuitionFee, bool>> predicate)
    {
        // Convert domain predicate to infrastructure predicate
        var parameter = Expression.Parameter(typeof(InfraTuitionFee), "tf");
        var visitor = new PredicateExpressionVisitor<DomainTuitionFee, InfraTuitionFee>(parameter);
        var infraPredicate = Expression.Lambda<Func<InfraTuitionFee, bool>>(
            visitor.Visit(predicate.Body), parameter);

        return await _context.TuitionFees.AnyAsync(infraPredicate);
    }

    public async Task<IEnumerable<DomainTuitionFee>> FindAsync(Expression<Func<DomainTuitionFee, bool>> predicate)
    {
        // Convert domain predicate to infrastructure predicate
        var parameter = Expression.Parameter(typeof(InfraTuitionFee), "tf");
        var visitor = new PredicateExpressionVisitor<DomainTuitionFee, InfraTuitionFee>(parameter);
        var infraPredicate = Expression.Lambda<Func<InfraTuitionFee, bool>>(
            visitor.Visit(predicate.Body), parameter);

        var infraFees = await _context.TuitionFees.Where(infraPredicate).ToListAsync();
        return infraFees.Select(MapToDomain);
    }

    private DomainTuitionFee MapToDomain(InfraTuitionFee infraFee)
    {
        return new DomainTuitionFee
        {
            TuitionFeeId = infraFee.TuitionFeeId,
            MajorId = infraFee.MajorId,
            MajorName = infraFee.MajorName,
            CampusId = infraFee.CampusId,
            CampusName = infraFee.CampusName,
            EnrollmentYearId = infraFee.EnrollmentYearId,
            EnrollmentYear = infraFee.EnrollmentYearName,
            Region = infraFee.Region,
            FeeType = infraFee.FeeType,
            BaseAmount = infraFee.BaseAmount,
            CampusDiscountPercent = infraFee.CampusDiscountPercent,
            SemesterIncreaseRules = infraFee.SemesterIncreaseRules,
            Currency = infraFee.Currency,
            Description = infraFee.Description,
            Notes = infraFee.Notes,
            EffectiveFrom = infraFee.EffectiveFrom,
            EffectiveTo = infraFee.EffectiveTo,
            IsActive = infraFee.IsActive,
            CreatedAt = infraFee.CreatedAt,
            UpdatedAt = infraFee.UpdatedAt
        };
    }

    private InfraTuitionFee MapToInfra(DomainTuitionFee domainFee)
    {
        return new InfraTuitionFee
        {
            TuitionFeeId = domainFee.TuitionFeeId,
            MajorId = domainFee.MajorId,
            MajorName = domainFee.MajorName,
            CampusId = domainFee.CampusId,
            CampusName = domainFee.CampusName,
            EnrollmentYearId = domainFee.EnrollmentYearId,
            EnrollmentYearName = domainFee.EnrollmentYear,
            Region = domainFee.Region,
            FeeType = domainFee.FeeType,
            BaseAmount = domainFee.BaseAmount,
            CampusDiscountPercent = domainFee.CampusDiscountPercent,
            SemesterIncreaseRules = domainFee.SemesterIncreaseRules,
            Currency = domainFee.Currency,
            Description = domainFee.Description,
            Notes = domainFee.Notes,
            EffectiveFrom = domainFee.EffectiveFrom,
            EffectiveTo = domainFee.EffectiveTo,
            IsActive = domainFee.IsActive,
            CreatedAt = domainFee.CreatedAt,
            UpdatedAt = domainFee.UpdatedAt
        };
    }

    // Helper class for expression conversion
    private class PredicateExpressionVisitor<TSource, TTarget> : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        public PredicateExpressionVisitor(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameter;
        }
    }
}

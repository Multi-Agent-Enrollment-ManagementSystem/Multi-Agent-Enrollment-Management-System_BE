using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface ITuitionFeeRepository : IGenericRepository<TuitionFee>
{
    /// <summary>
    /// Get tuition fees by major ID
    /// </summary>
    Task<IEnumerable<TuitionFee>> GetByMajorIdAsync(int majorId);

    /// <summary>
    /// Get tuition fees by campus ID
    /// </summary>
    Task<IEnumerable<TuitionFee>> GetByCampusIdAsync(int campusId);

    /// <summary>
    /// Get tuition fees by enrollment year ID
    /// </summary>
    Task<IEnumerable<TuitionFee>> GetByEnrollmentYearIdAsync(int enrollmentYearId);

    /// <summary>
    /// Get tuition fee for specific major, campus, and region
    /// </summary>
    Task<TuitionFee?> GetByMajorCampusRegionAsync(int majorId, int campusId, string region, int? enrollmentYearId = null);

    /// <summary>
    /// Get all active tuition fees
    /// </summary>
    Task<IEnumerable<TuitionFee>> GetActiveFeesAsync();

    /// <summary>
    /// Get tuition fees by fee type (REGULAR, ORIENTATION, ENGLISH_PREP)
    /// </summary>
    Task<IEnumerable<TuitionFee>> GetByFeeTypeAsync(string feeType);

    /// <summary>
    /// Calculate actual tuition fee for a specific semester
    /// </summary>
    /// <param name="baseFee">Base tuition fee entity</param>
    /// <param name="semesterNumber">Semester number (1-8+)</param>
    /// <returns>Calculated amount in VND</returns>
    decimal CalculateSemesterFee(TuitionFee baseFee, int semesterNumber);
}

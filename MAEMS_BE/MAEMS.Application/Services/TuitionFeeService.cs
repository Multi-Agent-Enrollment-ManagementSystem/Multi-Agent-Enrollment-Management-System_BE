using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;

namespace MAEMS.Application.Services;

/// <summary>
/// Helper service for calculating and querying tuition fees
/// Designed to be used by RAG chatbox for answering tuition-related questions
/// </summary>
public class TuitionFeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public TuitionFeeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Get tuition fee for a specific major at a campus and region
    /// Example: "Học phí ngành Công nghệ thông tin tại Hà Nội khu vực 1?"
    /// </summary>
    public async Task<TuitionFeeResult?> GetTuitionFeeAsync(
        string majorName,
        string campusName,
        string region = "OTHER",
        int? enrollmentYearId = null)
    {
        // Get all active fees matching criteria
        var fees = await _unitOfWork.TuitionFees.GetActiveFeesAsync();

        var matchingFee = fees.FirstOrDefault(f =>
            f.MajorName != null && f.MajorName.Contains(majorName, StringComparison.OrdinalIgnoreCase) &&
            f.CampusName != null && f.CampusName.Contains(campusName, StringComparison.OrdinalIgnoreCase) &&
            f.Region.Equals(region, StringComparison.OrdinalIgnoreCase) &&
            f.FeeType == "REGULAR" &&
            (!enrollmentYearId.HasValue || f.EnrollmentYearId == enrollmentYearId));

        if (matchingFee == null)
            return null;

        return new TuitionFeeResult
        {
            TuitionFee = matchingFee,
            Semester1Fee = CalculateSemesterFee(matchingFee, 1),
            Semester4Fee = CalculateSemesterFee(matchingFee, 4),
            Semester7Fee = CalculateSemesterFee(matchingFee, 7),
            TotalEstimate = CalculateTotalEstimate(matchingFee, 9) // 9 semesters for FPT University
        };
    }

    /// <summary>
    /// Calculate tuition fee for a specific semester with all discounts applied
    /// </summary>
    public decimal CalculateSemesterFee(TuitionFee baseFee, int semesterNumber)
    {
        if (semesterNumber < 1)
            throw new ArgumentException("Semester number must be greater than 0", nameof(semesterNumber));

        decimal amount = baseFee.BaseAmount;

        // Apply campus discount first
        if (baseFee.CampusDiscountPercent.HasValue && baseFee.CampusDiscountPercent.Value > 0)
        {
            amount = amount * (1 - baseFee.CampusDiscountPercent.Value / 100);
        }

        // Apply semester increase rules
        if (semesterNumber >= 1 && semesterNumber <= 3)
        {
            // HK1-3: No increase
            return Math.Round(amount, 0);
        }
        else if (semesterNumber >= 4 && semesterNumber <= 6)
        {
            // HK4-6: Increase 6.3%
            return Math.Round(amount * 1.063m, 0);
        }
        else // semesterNumber >= 7
        {
            // HK7+: First apply 6.3%, then apply 6.5% on the increased amount
            decimal hk4Amount = amount * 1.063m;
            return Math.Round(hk4Amount * 1.065m, 0);
        }
    }

    /// <summary>
    /// Calculate total estimated tuition for full program (9 semesters default for FPT University)
    /// </summary>
    public decimal CalculateTotalEstimate(TuitionFee baseFee, int totalSemesters = 9)
    {
        decimal total = 0;

        for (int sem = 1; sem <= totalSemesters; sem++)
        {
            total += CalculateSemesterFee(baseFee, sem);
        }

        return total;
    }

    /// <summary>
    /// Get orientation fee for a campus and region
    /// Example: "Học phí định hướng tại Hà Nội là bao nhiêu?"
    /// </summary>
    public async Task<decimal?> GetOrientationFeeAsync(string campusName, string region = "OTHER")
    {
        var fees = await _unitOfWork.TuitionFees.GetByFeeTypeAsync("ORIENTATION");

        var orientationFee = fees.FirstOrDefault(f =>
            f.CampusName != null && f.CampusName.Contains(campusName, StringComparison.OrdinalIgnoreCase) &&
            f.Region.Equals(region, StringComparison.OrdinalIgnoreCase));

        if (orientationFee == null)
            return null;

        // Apply campus discount
        decimal amount = orientationFee.BaseAmount;
        if (orientationFee.CampusDiscountPercent.HasValue && orientationFee.CampusDiscountPercent.Value > 0)
        {
            amount = amount * (1 - orientationFee.CampusDiscountPercent.Value / 100);
        }

        return Math.Round(amount, 0);
    }

    /// <summary>
    /// Get English preparation fee per level
    /// Example: "Học phí tiếng Anh mỗi mức là bao nhiêu?"
    /// </summary>
    public async Task<decimal?> GetEnglishPrepFeeAsync(string campusName, string region = "OTHER")
    {
        var fees = await _unitOfWork.TuitionFees.GetByFeeTypeAsync("ENGLISH_PREP");

        var englishFee = fees.FirstOrDefault(f =>
            f.CampusName != null && f.CampusName.Contains(campusName, StringComparison.OrdinalIgnoreCase) &&
            f.Region.Equals(region, StringComparison.OrdinalIgnoreCase));

        if (englishFee == null)
            return null;

        // Apply campus discount
        decimal amount = englishFee.BaseAmount;
        if (englishFee.CampusDiscountPercent.HasValue && englishFee.CampusDiscountPercent.Value > 0)
        {
            amount = amount * (1 - englishFee.CampusDiscountPercent.Value / 100);
        }

        return Math.Round(amount, 0);
    }

    /// <summary>
    /// Calculate total cost estimate including orientation, English prep, and regular tuition
    /// Example: "Tổng chi phí học CNTT tại Hà Nội là bao nhiêu?"
    /// </summary>
    public async Task<CompleteTuitionEstimate?> GetCompleteTuitionEstimateAsync(
        string majorName,
        string campusName,
        string region = "OTHER",
        int englishLevels = 0, // 0 if IELTS 6.0+, max 6 levels
        int totalSemesters = 9) // 9 semesters for FPT University
    {
        // Get regular tuition
        var regularFee = await GetTuitionFeeAsync(majorName, campusName, region);
        if (regularFee == null)
            return null;

        // Get orientation fee
        var orientationFee = await GetOrientationFeeAsync(campusName, region) ?? 0;

        // Get English prep fee
        var englishPrepPerLevel = await GetEnglishPrepFeeAsync(campusName, region) ?? 0;
        var totalEnglishPrep = englishPrepPerLevel * Math.Min(englishLevels, 6);

        return new CompleteTuitionEstimate
        {
            MajorName = majorName,
            CampusName = campusName,
            Region = region,
            OrientationFee = orientationFee,
            EnglishPrepLevels = englishLevels,
            EnglishPrepFeePerLevel = englishPrepPerLevel,
            TotalEnglishPrepFee = totalEnglishPrep,
            RegularTuitionTotal = regularFee.TotalEstimate,
            GrandTotal = orientationFee + totalEnglishPrep + regularFee.TotalEstimate,
            Semester1Fee = regularFee.Semester1Fee,
            Semester4Fee = regularFee.Semester4Fee,
            Semester7Fee = regularFee.Semester7Fee
        };
    }

    /// <summary>
    /// Compare tuition fees between different regions
    /// Example: "So sánh học phí khu vực 1 và các khu vực khác?"
    /// </summary>
    public async Task<TuitionFeeComparison?> CompareTuitionFeesAsync(
        string majorName,
        string campusName,
        int? enrollmentYearId = null)
    {
        var kv1Fee = await GetTuitionFeeAsync(majorName, campusName, "KV1", enrollmentYearId);
        var otherFee = await GetTuitionFeeAsync(majorName, campusName, "OTHER", enrollmentYearId);

        if (kv1Fee == null || otherFee == null)
            return null;

        return new TuitionFeeComparison
        {
            MajorName = majorName,
            CampusName = campusName,
            KV1Result = kv1Fee,
            OtherResult = otherFee,
            DifferenceSemester1 = otherFee.Semester1Fee - kv1Fee.Semester1Fee,
            DifferenceTotal = otherFee.TotalEstimate - kv1Fee.TotalEstimate
        };
    }

    /// <summary>
    /// Compare tuition fees across different campuses
    /// Example: "So sánh học phí Hà Nội và Quy Nhơn?"
    /// </summary>
    public async Task<List<CampusFeeComparison>> CompareCampusFeesAsync(
        string majorName,
        string region = "OTHER")
    {
        var campuses = new[] { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Quy Nhơn" };
        var results = new List<CampusFeeComparison>();

        foreach (var campus in campuses)
        {
            var fee = await GetTuitionFeeAsync(majorName, campus, region);
            if (fee != null)
            {
                results.Add(new CampusFeeComparison
                {
                    CampusName = campus,
                    Semester1Fee = fee.Semester1Fee,
                    TotalEstimate = fee.TotalEstimate,
                    DiscountPercent = fee.TuitionFee.CampusDiscountPercent ?? 0
                });
            }
        }

        return results;
    }
}

// DTOs for results
public class TuitionFeeResult
{
    public TuitionFee TuitionFee { get; set; } = null!;
    public decimal Semester1Fee { get; set; }
    public decimal Semester4Fee { get; set; }
    public decimal Semester7Fee { get; set; }
    public decimal TotalEstimate { get; set; }
}

public class TuitionFeeComparison
{
    public string MajorName { get; set; } = string.Empty;
    public string CampusName { get; set; } = string.Empty;
    public TuitionFeeResult KV1Result { get; set; } = null!;
    public TuitionFeeResult OtherResult { get; set; } = null!;
    public decimal DifferenceSemester1 { get; set; }
    public decimal DifferenceTotal { get; set; }
}

public class CampusFeeComparison
{
    public string CampusName { get; set; } = string.Empty;
    public decimal Semester1Fee { get; set; }
    public decimal TotalEstimate { get; set; }
    public decimal DiscountPercent { get; set; }
}

public class CompleteTuitionEstimate
{
    public string MajorName { get; set; } = string.Empty;
    public string CampusName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal OrientationFee { get; set; }
    public int EnglishPrepLevels { get; set; }
    public decimal EnglishPrepFeePerLevel { get; set; }
    public decimal TotalEnglishPrepFee { get; set; }
    public decimal RegularTuitionTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal Semester1Fee { get; set; }
    public decimal Semester4Fee { get; set; }
    public decimal Semester7Fee { get; set; }
}

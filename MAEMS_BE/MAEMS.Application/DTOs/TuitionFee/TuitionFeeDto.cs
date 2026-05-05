namespace MAEMS.Application.DTOs.TuitionFee;

public class TuitionFeeDto
{
    public int TuitionFeeId { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public int? CampusId { get; set; }
    public string? CampusName { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? EnrollmentYear { get; set; }
    public string Region { get; set; } = "OTHER";
    public string FeeType { get; set; } = "REGULAR";
    public decimal BaseAmount { get; set; }
    public decimal? CampusDiscountPercent { get; set; }
    public string? SemesterIncreaseRules { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTuitionFeeRequest
{
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public int? CampusId { get; set; }
    public string? CampusName { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? EnrollmentYear { get; set; }
    public string Region { get; set; } = "OTHER";
    public string FeeType { get; set; } = "REGULAR";
    public decimal BaseAmount { get; set; }
    public decimal? CampusDiscountPercent { get; set; }
    public string? SemesterIncreaseRules { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateTuitionFeeRequest
{
    public int TuitionFeeId { get; set; }
    public int? MajorId { get; set; }
    public string? MajorName { get; set; }
    public int? CampusId { get; set; }
    public string? CampusName { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? EnrollmentYear { get; set; }
    public string Region { get; set; } = "OTHER";
    public string FeeType { get; set; } = "REGULAR";
    public decimal BaseAmount { get; set; }
    public decimal? CampusDiscountPercent { get; set; }
    public string? SemesterIncreaseRules { get; set; }
    public string Currency { get; set; } = "VND";
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool? IsActive { get; set; }
}

public class TuitionFeeQueryRequest
{
    public string? MajorName { get; set; }
    public string? CampusName { get; set; }
    public string? Region { get; set; }
    public string? FeeType { get; set; }
    public int? EnrollmentYearId { get; set; }
}

public class TuitionFeeCalculationResponse
{
    public TuitionFeeDto TuitionFee { get; set; } = null!;
    public decimal Semester1Fee { get; set; }
    public decimal Semester4Fee { get; set; }
    public decimal Semester7Fee { get; set; }
    public decimal TotalEstimate8Semesters { get; set; }
    public string FormattedSemester1 { get; set; } = string.Empty;
    public string FormattedSemester4 { get; set; } = string.Empty;
    public string FormattedSemester7 { get; set; } = string.Empty;
    public string FormattedTotal { get; set; } = string.Empty;
}

public class TuitionFeeComparisonResponse
{
    public string MajorName { get; set; } = string.Empty;
    public string CampusName { get; set; } = string.Empty;
    public TuitionFeeCalculationResponse? KV1Result { get; set; }
    public TuitionFeeCalculationResponse? OtherResult { get; set; }
    public decimal DifferenceSemester1 { get; set; }
    public decimal DifferenceTotal { get; set; }
    public string FormattedDifferenceSemester1 { get; set; } = string.Empty;
    public string FormattedDifferenceTotal { get; set; } = string.Empty;
}

public class CampusFeeComparisonResponse
{
    public string CampusName { get; set; } = string.Empty;
    public decimal Semester1Fee { get; set; }
    public decimal TotalEstimate { get; set; }
    public decimal DiscountPercent { get; set; }
    public string FormattedSemester1Fee { get; set; } = string.Empty;
    public string FormattedTotalEstimate { get; set; } = string.Empty;
}

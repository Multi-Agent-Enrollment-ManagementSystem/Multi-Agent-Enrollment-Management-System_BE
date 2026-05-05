using System;

namespace MAEMS.Domain.Entities;

/// <summary>
/// Domain entity for tuition fees
/// Maps to Infrastructure.Models.TuitionFee
/// FPT University has 9 semesters with fee increases at HK4 (+6.3%) and HK7 (+6.5%)
/// </summary>
public class TuitionFee
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

    // Navigation properties
    public virtual Major? Major { get; set; }
    public virtual Campus? Campus { get; set; }
}

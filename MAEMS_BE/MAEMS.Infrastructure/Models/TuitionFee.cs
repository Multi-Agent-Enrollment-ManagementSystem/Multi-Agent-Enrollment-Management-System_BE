using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace MAEMS.Infrastructure.Models;

[Table("tuition_fees")]
public partial class TuitionFee
{
    [Key]
    [Column("tuition_fee_id")]
    public int TuitionFeeId { get; set; }

    [Column("major_id")]
    public int? MajorId { get; set; }

    [Column("major_name")]
    [StringLength(255)]
    public string? MajorName { get; set; }

    [Column("campus_id")]
    public int? CampusId { get; set; }

    [Column("campus_name")]
    [StringLength(255)]
    public string? CampusName { get; set; }

    [Column("enrollment_year_id")]
    public int? EnrollmentYearId { get; set; }

    [Column("enrollment_year")]
    [StringLength(50)]
    public string? EnrollmentYearName { get; set; }

    [Column("region")]
    [StringLength(20)]
    public string Region { get; set; } = "OTHER";

    [Column("fee_type")]
    [StringLength(50)]
    public string FeeType { get; set; } = "REGULAR";

    [Column("base_amount")]
    [Precision(18, 2)]
    public decimal BaseAmount { get; set; }

    [Column("campus_discount_percent")]
    [Precision(5, 2)]
    public decimal? CampusDiscountPercent { get; set; }

    [Column("semester_increase_rules")]
    [StringLength(500)]
    public string? SemesterIncreaseRules { get; set; }

    [Column("currency")]
    [StringLength(10)]
    public string Currency { get; set; } = "VND";

    [Column("description")]
    public string? Description { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("effective_from")]
    public DateTime? EffectiveFrom { get; set; }

    [Column("effective_to")]
    public DateTime? EffectiveTo { get; set; }

    [Column("is_active")]
    public bool? IsActive { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("MajorId")]
    [InverseProperty("TuitionFees")]
    public virtual Major? Major { get; set; }

    [ForeignKey("CampusId")]
    [InverseProperty("TuitionFees")]
    public virtual Campus? Campus { get; set; }

    [ForeignKey("EnrollmentYearId")]
    [InverseProperty("TuitionFees")]
    public virtual EnrollmentYear? EnrollmentYear { get; set; }
}

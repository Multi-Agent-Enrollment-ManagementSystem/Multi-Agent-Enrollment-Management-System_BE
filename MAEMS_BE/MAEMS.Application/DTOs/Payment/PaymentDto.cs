namespace MAEMS.Application.DTOs.Payment;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int? ApplicationId { get; set; }
    public int? ApplicantId { get; set; }
    public decimal? Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAt { get; set; }
}

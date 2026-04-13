namespace MAEMS.Application.DTOs.Report;

public sealed class PaidRevenueByQuarterDto
{
    public int Year { get; set; }

    // Number of payments currently having status = Need_checking
    public int NumPaymentNeedCheck { get; set; }

    public IReadOnlyList<PaidRevenueQuarterItemDto> Quarters { get; set; } = Array.Empty<PaidRevenueQuarterItemDto>();
}

public sealed class PaidRevenueQuarterItemDto
{
    public int Quarter { get; set; } // 1..4
    public decimal TotalAmount { get; set; }
}

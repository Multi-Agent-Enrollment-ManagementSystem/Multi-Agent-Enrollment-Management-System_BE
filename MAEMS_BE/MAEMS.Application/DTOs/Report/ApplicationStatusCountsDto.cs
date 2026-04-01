namespace MAEMS.Application.DTOs.Report;

public sealed class ApplicationStatusCountsDto
{
    public int NumApproved { get; set; }
    public int NumRejected { get; set; }
    public int NumPending { get; set; }
}

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetReportSummary;

public sealed class GetReportSummaryQueryHandler : IRequestHandler<GetReportSummaryQuery, BaseResponse<ReportSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReportSummaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<ReportSummaryDto>> Handle(GetReportSummaryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var numApplicant = await _unitOfWork.Payments.CountDistinctPaidApplicantsAsync(cancellationToken);
            var numApplication = await _unitOfWork.Payments.CountDistinctPaidApplicationsAsync(cancellationToken);
            var numPaymentNeedCheck = await _unitOfWork.Payments.CountNeedCheckingPaymentsAsync(cancellationToken);

            // Active programs (already filtered in SQL inside repository)
            var activePrograms = await _unitOfWork.Programs.GetActiveProgramsAsync();
            var numProgram = activePrograms.Count();

            var dto = new ReportSummaryDto
            {
                NumApplicant = numApplicant,
                NumApplication = numApplication,
                NumPaymentNeedCheck = numPaymentNeedCheck,
                NumProgram = numProgram
            };

            return BaseResponse<ReportSummaryDto>.SuccessResponse(dto, "Report summary retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ReportSummaryDto>.FailureResponse(
                "Error retrieving report summary",
                new List<string> { ex.Message });
        }
    }
}

using MAEMS.Application.DTOs.Report;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Reports.Queries.GetWeeklySubmittedApplicationsCount;

public sealed class GetWeeklySubmittedApplicationsCountQueryHandler
    : IRequestHandler<GetWeeklySubmittedApplicationsCountQuery, BaseResponse<List<WeeklyApplicationsCountDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetWeeklySubmittedApplicationsCountQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<WeeklyApplicationsCountDto>>> Handle(
        GetWeeklySubmittedApplicationsCountQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _unitOfWork.Applications.CountNonDraftByWeekAsync(request.From, request.To, cancellationToken);

            var data = rows
                .Select(x => new WeeklyApplicationsCountDto
                {
                    WeekStart = x.WeekStart,
                    Count = x.Count
                })
                .ToList();

            return BaseResponse<List<WeeklyApplicationsCountDto>>.SuccessResponse(
                data,
                "Weekly submitted applications count retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<List<WeeklyApplicationsCountDto>>.FailureResponse(
                "Error retrieving weekly submitted applications count",
                new List<string> { ex.Message });
        }
    }
}

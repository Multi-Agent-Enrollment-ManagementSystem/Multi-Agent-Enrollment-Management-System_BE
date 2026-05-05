using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;

namespace MAEMS.Application.Features.TuitionFees.Queries.CompareCampusFees;

public class CompareCampusFeesQueryHandler : IRequestHandler<CompareCampusFeesQuery, BaseResponse<IEnumerable<CampusFeeComparisonResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CompareCampusFeesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<IEnumerable<CampusFeeComparisonResponse>>> Handle(CompareCampusFeesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var campuses = new[] { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Quy Nhơn" };
            var results = new List<CampusFeeComparisonResponse>();

            var allFees = await _unitOfWork.TuitionFees.GetActiveFeesAsync();

            foreach (var campus in campuses)
            {
                var matchingFee = allFees.FirstOrDefault(f =>
                    f.MajorName != null && f.MajorName.Contains(request.MajorName, StringComparison.OrdinalIgnoreCase) &&
                    f.CampusName != null && f.CampusName.Contains(campus, StringComparison.OrdinalIgnoreCase) &&
                    f.Region.Equals(request.Region, StringComparison.OrdinalIgnoreCase) &&
                    f.FeeType == "REGULAR");

                if (matchingFee != null)
                {
                    var semester1Fee = _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, 1);

                    decimal total = 0;
                    for (int sem = 1; sem <= 8; sem++)
                    {
                        total += _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, sem);
                    }

                    results.Add(new CampusFeeComparisonResponse
                    {
                        CampusName = campus,
                        Semester1Fee = semester1Fee,
                        TotalEstimate = total,
                        DiscountPercent = matchingFee.CampusDiscountPercent ?? 0,
                        FormattedSemester1Fee = $"{semester1Fee:N0} VNĐ",
                        FormattedTotalEstimate = $"{total:N0} VNĐ"
                    });
                }
            }

            if (!results.Any())
            {
                return new BaseResponse<IEnumerable<CampusFeeComparisonResponse>>
                {
                    Success = false,
                    Message = $"Không tìm thấy học phí cho ngành '{request.MajorName}'"
                };
            }

            return new BaseResponse<IEnumerable<CampusFeeComparisonResponse>>
            {
                Success = true,
                Message = "So sánh học phí giữa các campus thành công",
                Data = results
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse<IEnumerable<CampusFeeComparisonResponse>>
            {
                Success = false,
                Message = $"Lỗi khi so sánh học phí: {ex.Message}"
            };
        }
    }
}

using AutoMapper;
using MediatR;
using MAEMS.Application.DTOs.TuitionFee;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;

namespace MAEMS.Application.Features.TuitionFees.Queries.GetTuitionFeeCalculation;

public class GetTuitionFeeCalculationQueryHandler : IRequestHandler<GetTuitionFeeCalculationQuery, BaseResponse<TuitionFeeCalculationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetTuitionFeeCalculationQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<TuitionFeeCalculationResponse>> Handle(GetTuitionFeeCalculationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get all active fees
            var fees = await _unitOfWork.TuitionFees.GetActiveFeesAsync();

            // Find matching fee
            var matchingFee = fees.FirstOrDefault(f =>
                f.MajorName != null && f.MajorName.Contains(request.MajorName, StringComparison.OrdinalIgnoreCase) &&
                f.CampusName != null && f.CampusName.Contains(request.CampusName, StringComparison.OrdinalIgnoreCase) &&
                f.Region.Equals(request.Region, StringComparison.OrdinalIgnoreCase) &&
                f.FeeType == "REGULAR" &&
                (!request.EnrollmentYearId.HasValue || f.EnrollmentYearId == request.EnrollmentYearId));

            if (matchingFee == null)
            {
                return new BaseResponse<TuitionFeeCalculationResponse>
                {
                    Success = false,
                    Message = $"Không tìm thấy học phí cho ngành '{request.MajorName}' tại '{request.CampusName}' khu vực '{request.Region}'"
                };
            }

            // Calculate fees for different semesters
            var semester1Fee = _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, 1);
            var semester4Fee = _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, 4);
            var semester7Fee = _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, 7);

            // Calculate total for 8 semesters
            decimal total = 0;
            for (int sem = 1; sem <= 8; sem++)
            {
                total += _unitOfWork.TuitionFees.CalculateSemesterFee(matchingFee, sem);
            }

            var response = new TuitionFeeCalculationResponse
            {
                TuitionFee = _mapper.Map<TuitionFeeDto>(matchingFee),
                Semester1Fee = semester1Fee,
                Semester4Fee = semester4Fee,
                Semester7Fee = semester7Fee,
                TotalEstimate8Semesters = total,
                FormattedSemester1 = FormatCurrency(semester1Fee),
                FormattedSemester4 = FormatCurrency(semester4Fee),
                FormattedSemester7 = FormatCurrency(semester7Fee),
                FormattedTotal = FormatCurrency(total)
            };

            return new BaseResponse<TuitionFeeCalculationResponse>
            {
                Success = true,
                Message = "Lấy thông tin học phí thành công",
                Data = response
            };
        }
        catch (Exception ex)
        {
            return new BaseResponse<TuitionFeeCalculationResponse>
            {
                Success = false,
                Message = $"Lỗi khi tính học phí: {ex.Message}"
            };
        }
    }

    private string FormatCurrency(decimal amount)
    {
        return $"{amount:N0} VNĐ";
    }
}

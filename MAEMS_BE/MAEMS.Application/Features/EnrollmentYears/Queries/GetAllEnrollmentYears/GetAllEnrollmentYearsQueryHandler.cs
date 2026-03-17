using AutoMapper;
using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Queries.GetAllEnrollmentYears;

public class GetAllEnrollmentYearsQueryHandler : IRequestHandler<GetAllEnrollmentYearsQuery, BaseResponse<IEnumerable<EnrollmentYearDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllEnrollmentYearsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<EnrollmentYearDto>>> Handle(GetAllEnrollmentYearsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var years = await _unitOfWork.EnrollmentYears.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<EnrollmentYearDto>>(years);
            return BaseResponse<IEnumerable<EnrollmentYearDto>>.SuccessResponse(dtos, "Enrollment years retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<EnrollmentYearDto>>.FailureResponse(
                "Error retrieving enrollment years",
                new List<string> { ex.Message }
            );
        }
    }
}

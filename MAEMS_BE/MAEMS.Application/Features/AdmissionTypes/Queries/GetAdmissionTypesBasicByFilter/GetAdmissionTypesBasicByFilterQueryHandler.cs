using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypesBasicByFilter;

public class GetAdmissionTypesBasicByFilterQueryHandler : IRequestHandler<GetAdmissionTypesBasicByFilterQuery, BaseResponse<IEnumerable<AdmissionTypeBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdmissionTypesBasicByFilterQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<AdmissionTypeBasicDto>>> Handle(GetAdmissionTypesBasicByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<MAEMS.Domain.Entities.AdmissionType> admissionTypes;

            // Filter by EnrollmentYearId if provided
            if (request.EnrollmentYearId.HasValue)
            {
                admissionTypes = await _unitOfWork.AdmissionTypes.GetAdmissionTypesByEnrollmentYearIdAsync(request.EnrollmentYearId.Value);
                // Filter only active admission types
                admissionTypes = admissionTypes.Where(at => at.IsActive == true);
            }
            else
            {
                // Get all active admission types if no enrollment year filter
                admissionTypes = await _unitOfWork.AdmissionTypes.GetActiveAdmissionTypesAsync();
            }

            var admissionTypeDtos = _mapper.Map<IEnumerable<AdmissionTypeBasicDto>>(admissionTypes);

            return BaseResponse<IEnumerable<AdmissionTypeBasicDto>>.SuccessResponse(
                admissionTypeDtos,
                $"Admission types retrieved successfully. Found {admissionTypeDtos.Count()} admission type(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<AdmissionTypeBasicDto>>.FailureResponse(
                "Error retrieving admission types",
                new List<string> { ex.Message }
            );
        }
    }
}

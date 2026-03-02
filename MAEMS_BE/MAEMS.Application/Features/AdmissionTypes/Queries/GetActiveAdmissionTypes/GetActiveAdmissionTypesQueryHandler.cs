using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetActiveAdmissionTypes;

public class GetActiveAdmissionTypesQueryHandler : IRequestHandler<GetActiveAdmissionTypesQuery, BaseResponse<IEnumerable<AdmissionTypeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveAdmissionTypesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<AdmissionTypeDto>>> Handle(GetActiveAdmissionTypesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var admissionTypes = await _unitOfWork.AdmissionTypes.GetActiveAdmissionTypesAsync();
            var admissionTypeDtos = _mapper.Map<IEnumerable<AdmissionTypeDto>>(admissionTypes);

            return BaseResponse<IEnumerable<AdmissionTypeDto>>.SuccessResponse(
                admissionTypeDtos,
                $"Active admission types retrieved successfully. Found {admissionTypeDtos.Count()} admission type(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<AdmissionTypeDto>>.FailureResponse(
                "Error retrieving active admission types",
                new List<string> { ex.Message }
            );
        }
    }
}

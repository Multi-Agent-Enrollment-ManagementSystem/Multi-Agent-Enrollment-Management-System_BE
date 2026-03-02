using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAllAdmissionTypes;

public class GetAllAdmissionTypesQueryHandler : IRequestHandler<GetAllAdmissionTypesQuery, BaseResponse<IEnumerable<AdmissionTypeDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllAdmissionTypesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<AdmissionTypeDto>>> Handle(GetAllAdmissionTypesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var admissionTypes = await _unitOfWork.AdmissionTypes.GetAllAsync();
            var admissionTypeDtos = _mapper.Map<IEnumerable<AdmissionTypeDto>>(admissionTypes);

            return BaseResponse<IEnumerable<AdmissionTypeDto>>.SuccessResponse(
                admissionTypeDtos,
                $"All admission types retrieved successfully. Found {admissionTypeDtos.Count()} admission type(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<AdmissionTypeDto>>.FailureResponse(
                "Error retrieving all admission types",
                new List<string> { ex.Message }
            );
        }
    }
}

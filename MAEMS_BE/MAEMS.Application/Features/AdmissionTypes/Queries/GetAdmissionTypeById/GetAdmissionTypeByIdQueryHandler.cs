using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypeById;

public class GetAdmissionTypeByIdQueryHandler : IRequestHandler<GetAdmissionTypeByIdQuery, BaseResponse<AdmissionTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdmissionTypeByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<AdmissionTypeDto>> Handle(GetAdmissionTypeByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var admissionType = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.Id);

            if (admissionType == null)
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    "Admission type not found",
                    new List<string> { $"No admission type found with ID: {request.Id}" }
                );
            }

            var admissionTypeDto = _mapper.Map<AdmissionTypeDto>(admissionType);

            return BaseResponse<AdmissionTypeDto>.SuccessResponse(
                admissionTypeDto,
                "Admission type retrieved successfully.");
        }
        catch (Exception ex)
        {
            return BaseResponse<AdmissionTypeDto>.FailureResponse(
                "Error retrieving admission type",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigById;

public class GetProgramAdmissionConfigByIdQueryHandler
    : IRequestHandler<GetProgramAdmissionConfigByIdQuery, BaseResponse<ProgramAdmissionConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProgramAdmissionConfigByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramAdmissionConfigDto>> Handle(
        GetProgramAdmissionConfigByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var config = await _unitOfWork.ProgramAdmissionConfigs.GetByIdAsync(request.ConfigId);

            if (config == null)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Program admission config not found",
                    new List<string> { $"No config found with ID {request.ConfigId}" });
            }

            var dto = _mapper.Map<ProgramAdmissionConfigDto>(config);

            return BaseResponse<ProgramAdmissionConfigDto>.SuccessResponse(
                dto,
                "Program admission config retrieved successfully.");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                "Error retrieving program admission config",
                new List<string> { ex.Message });
        }
    }
}

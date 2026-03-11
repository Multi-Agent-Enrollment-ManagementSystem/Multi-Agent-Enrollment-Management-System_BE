using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetActiveProgramAdmissionConfigs;

public class GetActiveProgramAdmissionConfigsQueryHandler
    : IRequestHandler<GetActiveProgramAdmissionConfigsQuery, BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveProgramAdmissionConfigsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>> Handle(
        GetActiveProgramAdmissionConfigsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var configs = await _unitOfWork.ProgramAdmissionConfigs.GetActiveConfigsAsync();
            var dtos = _mapper.Map<IEnumerable<ProgramAdmissionConfigDto>>(configs);

            return BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>.SuccessResponse(
                dtos,
                $"Active program admission configs retrieved successfully. Found {dtos.Count()} config(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>.FailureResponse(
                "Error retrieving active program admission configs",
                new List<string> { ex.Message });
        }
    }
}

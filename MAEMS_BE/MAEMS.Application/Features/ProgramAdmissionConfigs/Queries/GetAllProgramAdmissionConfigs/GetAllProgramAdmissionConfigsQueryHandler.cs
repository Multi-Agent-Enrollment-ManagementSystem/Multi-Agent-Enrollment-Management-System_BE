using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;

public class GetAllProgramAdmissionConfigsQueryHandler
    : IRequestHandler<GetAllProgramAdmissionConfigsQuery, BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllProgramAdmissionConfigsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>> Handle(
        GetAllProgramAdmissionConfigsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var configs = await _unitOfWork.ProgramAdmissionConfigs.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<ProgramAdmissionConfigDto>>(configs);

            return BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>.SuccessResponse(
                dtos,
                $"Program admission configs retrieved successfully. Found {dtos.Count()} config(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>.FailureResponse(
                "Error retrieving program admission configs",
                new List<string> { ex.Message });
        }
    }
}

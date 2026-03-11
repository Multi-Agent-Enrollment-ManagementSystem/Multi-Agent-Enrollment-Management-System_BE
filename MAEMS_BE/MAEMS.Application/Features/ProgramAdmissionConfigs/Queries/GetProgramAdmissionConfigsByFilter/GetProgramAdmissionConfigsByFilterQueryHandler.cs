using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetProgramAdmissionConfigsByFilter;

public class GetProgramAdmissionConfigsByFilterQueryHandler
    : IRequestHandler<GetProgramAdmissionConfigsByFilterQuery, BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProgramAdmissionConfigsByFilterQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramAdmissionConfigDto>>> Handle(
        GetProgramAdmissionConfigsByFilterQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<ProgramAdmissionConfig> configs;

            // Start from the most specific filter available
            if (request.ProgramId.HasValue)
                configs = await _unitOfWork.ProgramAdmissionConfigs.GetConfigsByProgramIdAsync(request.ProgramId.Value);
            else if (request.CampusId.HasValue)
                configs = await _unitOfWork.ProgramAdmissionConfigs.GetConfigsByCampusIdAsync(request.CampusId.Value);
            else if (request.AdmissionTypeId.HasValue)
                configs = await _unitOfWork.ProgramAdmissionConfigs.GetConfigsByAdmissionTypeIdAsync(request.AdmissionTypeId.Value);
            else
                configs = await _unitOfWork.ProgramAdmissionConfigs.GetAllAsync();

            // Apply secondary in-memory filters
            if (request.CampusId.HasValue)
                configs = configs.Where(c => c.CampusId == request.CampusId.Value);

            if (request.AdmissionTypeId.HasValue)
                configs = configs.Where(c => c.AdmissionTypeId == request.AdmissionTypeId.Value);

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

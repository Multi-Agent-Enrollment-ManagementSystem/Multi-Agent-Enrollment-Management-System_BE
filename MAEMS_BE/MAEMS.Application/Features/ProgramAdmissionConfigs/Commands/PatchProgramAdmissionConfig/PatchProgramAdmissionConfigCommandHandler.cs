using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.PatchProgramAdmissionConfig;

public class PatchProgramAdmissionConfigCommandHandler : IRequestHandler<PatchProgramAdmissionConfigCommand, BaseResponse<ProgramAdmissionConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchProgramAdmissionConfigCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramAdmissionConfigDto>> Handle(PatchProgramAdmissionConfigCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _unitOfWork.ProgramAdmissionConfigs.GetByIdAsync(request.ConfigId);
            if (entity == null)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    $"Config with ID {request.ConfigId} not found",
                    new List<string> { "Program admission config not found" }
                );
            }

            if (request.Quota.HasValue && request.Quota.Value < 0)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "Quota must be >= 0" }
                );
            }

            // Apply changes
            if (request.ProgramId.HasValue)
            {
                var program = await _unitOfWork.Programs.GetByIdAsync(request.ProgramId.Value);
                if (program == null)
                {
                    return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                        "Invalid program",
                        new List<string> { "Program not found" }
                    );
                }
                entity.ProgramId = request.ProgramId;
                entity.ProgramName = program.ProgramName;
            }

            if (request.CampusId.HasValue)
            {
                var campus = await _unitOfWork.Campuses.GetByIdAsync(request.CampusId.Value);
                if (campus == null)
                {
                    return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                        "Invalid campus",
                        new List<string> { "Campus not found" }
                    );
                }
                entity.CampusId = request.CampusId;
                entity.CampusName = campus.Name;
            }

            if (request.AdmissionTypeId.HasValue)
            {
                var admissionType = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.AdmissionTypeId.Value);
                if (admissionType == null)
                {
                    return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                        "Invalid admission type",
                        new List<string> { "AdmissionType not found" }
                    );
                }
                entity.AdmissionTypeId = request.AdmissionTypeId;
                entity.AdmissionTypeName = admissionType.AdmissionTypeName;
            }

            if (request.Quota.HasValue)
                entity.Quota = request.Quota;

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive;

            // Prevent duplicates after patch
            var exists = await _unitOfWork.ProgramAdmissionConfigs.ExistsAsync(c =>
                c.ConfigId != request.ConfigId &&
                c.ProgramId == entity.ProgramId &&
                c.CampusId == entity.CampusId &&
                c.AdmissionTypeId == entity.AdmissionTypeId);

            if (exists)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Config already exists",
                    new List<string> { "A config with the same Program/Campus/AdmissionType already exists" }
                );
            }

            await _unitOfWork.ProgramAdmissionConfigs.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.ProgramAdmissionConfigs.GetByIdAsync(request.ConfigId);
            var dto = _mapper.Map<ProgramAdmissionConfigDto>(updated ?? entity);

            return BaseResponse<ProgramAdmissionConfigDto>.SuccessResponse(dto, "Program admission config updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                "Error updating program admission config",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Commands.CreateProgramAdmissionConfig;

public class CreateProgramAdmissionConfigCommandHandler : IRequestHandler<CreateProgramAdmissionConfigCommand, BaseResponse<ProgramAdmissionConfigDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProgramAdmissionConfigCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramAdmissionConfigDto>> Handle(CreateProgramAdmissionConfigCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.ProgramId.HasValue)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "ProgramId is required" }
                );
            }

            if (!request.CampusId.HasValue)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "CampusId is required" }
                );
            }

            if (!request.AdmissionTypeId.HasValue)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "AdmissionTypeId is required" }
                );
            }

            if (request.Quota.HasValue && request.Quota.Value < 0)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "Quota must be >= 0" }
                );
            }

            // Validate FK references exist
            var program = await _unitOfWork.Programs.GetByIdAsync(request.ProgramId.Value);
            if (program == null)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid program",
                    new List<string> { "Program not found" }
                );
            }

            var campus = await _unitOfWork.Campuses.GetByIdAsync(request.CampusId.Value);
            if (campus == null)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid campus",
                    new List<string> { "Campus not found" }
                );
            }

            var admissionType = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.AdmissionTypeId.Value);
            if (admissionType == null)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Invalid admission type",
                    new List<string> { "AdmissionType not found" }
                );
            }

            // Avoid duplicates for same (ProgramId, CampusId, AdmissionTypeId)
            var exists = await _unitOfWork.ProgramAdmissionConfigs.ExistsAsync(c =>
                c.ProgramId == request.ProgramId &&
                c.CampusId == request.CampusId &&
                c.AdmissionTypeId == request.AdmissionTypeId);

            if (exists)
            {
                return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                    "Config already exists",
                    new List<string> { "A config with the same Program/Campus/AdmissionType already exists" }
                );
            }

            var entity = new ProgramAdmissionConfig
            {
                ProgramId = request.ProgramId,
                CampusId = request.CampusId,
                AdmissionTypeId = request.AdmissionTypeId,
                Quota = request.Quota,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),

                // For convenience in response
                ProgramName = program.ProgramName,
                CampusName = campus.Name,
                AdmissionTypeName = admissionType.AdmissionTypeName
            };

            var created = await _unitOfWork.ProgramAdmissionConfigs.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Reload full config from repo if supported by GetByIdAsync mapping
            var createdFull = await _unitOfWork.ProgramAdmissionConfigs.GetByIdAsync(created.ConfigId);
            var dto = _mapper.Map<ProgramAdmissionConfigDto>(createdFull ?? created);

            return BaseResponse<ProgramAdmissionConfigDto>.SuccessResponse(dto, "Program admission config created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramAdmissionConfigDto>.FailureResponse(
                "Error creating program admission config",
                new List<string> { ex.Message }
            );
        }
    }
}

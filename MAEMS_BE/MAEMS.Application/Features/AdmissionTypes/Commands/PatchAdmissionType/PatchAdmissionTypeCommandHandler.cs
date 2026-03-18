using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Commands.PatchAdmissionType;

public class PatchAdmissionTypeCommandHandler : IRequestHandler<PatchAdmissionTypeCommand, BaseResponse<AdmissionTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchAdmissionTypeCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<AdmissionTypeDto>> Handle(PatchAdmissionTypeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.AdmissionTypeId);
            if (entity == null)
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    $"AdmissionType with ID {request.AdmissionTypeId} not found",
                    new List<string> { "Admission type not found" }
                );
            }

            if (request.AdmissionTypeName != null)
            {
                if (string.IsNullOrWhiteSpace(request.AdmissionTypeName))
                {
                    return BaseResponse<AdmissionTypeDto>.FailureResponse(
                        "Invalid admission type",
                        new List<string> { "AdmissionTypeName cannot be empty" }
                    );
                }

                entity.AdmissionTypeName = request.AdmissionTypeName.Trim();
            }

            if (request.EnrollmentYearId.HasValue)
                entity.EnrollmentYearId = request.EnrollmentYearId;

            if (request.Type != null)
            {
                if (string.IsNullOrWhiteSpace(request.Type))
                {
                    return BaseResponse<AdmissionTypeDto>.FailureResponse(
                        "Invalid admission type",
                        new List<string> { "Type cannot be empty" }
                    );
                }

                entity.Type = request.Type.Trim();
            }

            if (request.RequiredDocumentList != null)
                entity.RequiredDocumentList = request.RequiredDocumentList;

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive;

            // Optional: avoid duplicate after patch (name+year+type)
            var name = entity.AdmissionTypeName.Trim();
            var type = entity.Type.Trim();
            var exists = await _unitOfWork.AdmissionTypes.ExistsAsync(a =>
                a.AdmissionTypeId != request.AdmissionTypeId &&
                a.AdmissionTypeName.ToLower() == name.ToLower() &&
                a.EnrollmentYearId == entity.EnrollmentYearId &&
                a.Type.ToLower() == type.ToLower());

            if (exists)
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    "Admission type already exists",
                    new List<string> { "An admission type with the same name already exists" }
                );
            }

            await _unitOfWork.AdmissionTypes.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var updated = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.AdmissionTypeId);
            var dto = _mapper.Map<AdmissionTypeDto>(updated ?? entity);

            return BaseResponse<AdmissionTypeDto>.SuccessResponse(dto, "Admission type updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<AdmissionTypeDto>.FailureResponse(
                "Error updating admission type",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Commands.CreateAdmissionType;

public class CreateAdmissionTypeCommandHandler : IRequestHandler<CreateAdmissionTypeCommand, BaseResponse<AdmissionTypeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateAdmissionTypeCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<AdmissionTypeDto>> Handle(CreateAdmissionTypeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AdmissionTypeName))
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    "Invalid admission type",
                    new List<string> { "AdmissionTypeName is required" }
                );
            }

            if (string.IsNullOrWhiteSpace(request.Type))
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    "Invalid admission type",
                    new List<string> { "Type is required" }
                );
            }

            // EnrollmentYearId is optional from existing codebase patterns.
            // If provided, DB FK constraint will enforce validity.

            var name = request.AdmissionTypeName.Trim();
            var type = request.Type.Trim();

            // Optional: avoid duplicate name within same enrollment year + type
            var exists = await _unitOfWork.AdmissionTypes.ExistsAsync(a =>
                a.AdmissionTypeName.ToLower() == name.ToLower() &&
                a.EnrollmentYearId == request.EnrollmentYearId &&
                a.Type.ToLower() == type.ToLower());

            if (exists)
            {
                return BaseResponse<AdmissionTypeDto>.FailureResponse(
                    "Admission type already exists",
                    new List<string> { "An admission type with the same name already exists" }
                );
            }

            var entity = new AdmissionType
            {
                AdmissionTypeName = name,
                EnrollmentYearId = request.EnrollmentYearId,
                Type = type,
                RequiredDocumentList = request.RequiredDocumentList,
                EligibilityRules = request.EligibilityRules,
                PriorityRules = request.PriorityRules,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var created = await _unitOfWork.AdmissionTypes.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            // Reload with enrollment year text
            var createdWithYear = await _unitOfWork.AdmissionTypes.GetByIdAsync(created.AdmissionTypeId);
            var dto = _mapper.Map<AdmissionTypeDto>(createdWithYear ?? created);

            return BaseResponse<AdmissionTypeDto>.SuccessResponse(dto, "Admission type created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<AdmissionTypeDto>.FailureResponse(
                "Error creating admission type",
                new List<string> { ex.Message }
            );
        }
    }
}

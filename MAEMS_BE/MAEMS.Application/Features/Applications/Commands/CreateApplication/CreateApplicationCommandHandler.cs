using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.CreateApplication;

public class CreateApplicationCommandHandler : IRequestHandler<CreateApplicationCommand, BaseResponse<ApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateApplicationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicationDto>> Handle(CreateApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Kiểm tra xem applicant đã có application cho program này chưa
            var existingApplication = await _unitOfWork.Applications.GetByApplicantIdAsync(request.ApplicantId);
            if (existingApplication != null && existingApplication.ProgramId == request.ProgramId)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Application already exists",
                    new List<string> { "This applicant already has an application for this program" }
                );
            }

            // Validate foreign keys exist
            var applicant = await _unitOfWork.Applicants.GetByIdAsync(request.ApplicantId);
            if (applicant == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid applicant",
                    new List<string> { "Applicant not found" }
                );
            }

            var program = await _unitOfWork.Programs.GetByIdAsync(request.ProgramId);
            if (program == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid program",
                    new List<string> { "Program not found" }
                );
            }

            var campus = await _unitOfWork.Campuses.GetByIdAsync(request.CampusId);
            if (campus == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid campus",
                    new List<string> { "Campus not found" }
                );
            }

            var admissionType = await _unitOfWork.AdmissionTypes.GetByIdAsync(request.AdmissionTypeId);
            if (admissionType == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid admission type",
                    new List<string> { "Admission type not found" }
                );
            }

            // Tạo application mới
            var application = new Domain.Entities.Application
            {
                ApplicantId = request.ApplicantId,
                ProgramId = request.ProgramId,
                EnrollmentYearId = request.EnrollmentYearId,
                CampusId = request.CampusId,
                AdmissionTypeId = request.AdmissionTypeId,
                Status = "draft", // Default status
                RequiresReview = false, // Default value
                SubmittedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var createdApplication = await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();

            // Get application with details
            var applicationWithDetails = await _unitOfWork.Applications.GetByIdAsync(createdApplication.ApplicationId);
            var applicationDto = _mapper.Map<ApplicationDto>(applicationWithDetails);

            return BaseResponse<ApplicationDto>.SuccessResponse(applicationDto, "Application created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicationDto>.FailureResponse(
                "Error creating application",
                new List<string> { ex.Message }
            );
        }
    }
}
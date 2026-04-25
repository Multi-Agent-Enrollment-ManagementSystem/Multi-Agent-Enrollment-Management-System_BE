using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
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
            // Validate applicant tồn tại
            var applicant = await _unitOfWork.Applicants.GetByIdAsync(request.ApplicantId);
            if (applicant == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid applicant",
                    new List<string> { "Applicant not found" }
                );
            }

            // Validate config tồn tại và đang active
            var config = await _unitOfWork.ProgramAdmissionConfigs.GetByIdAsync(request.ConfigId);
            if (config == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "Program admission config not found" }
                );
            }
            if (config.IsActive != true)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid config",
                    new List<string> { "Program admission config is not active" }
                );
            }

            // Kiểm tra applicant đã có application cho config này chưa
            var existingApplications = await _unitOfWork.Applications.GetAllByApplicantIdAsync(request.ApplicantId);
            var duplicate = existingApplications.FirstOrDefault(a => a.ConfigId == request.ConfigId);
            if (duplicate != null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Application already exists",
                    new List<string> { "This applicant already has an application for this admission configuration" }
                );
            }

            // Tạo application mới — chỉ lưu ConfigId (FK)
            var application = new Domain.Entities.Application
            {
                ApplicantId = request.ApplicantId,
                ConfigId = request.ConfigId,
                ProgramId = config.ProgramId,
                CampusId = config.CampusId,
                AdmissionTypeId = config.AdmissionTypeId,
                Status = "draft",
                RequiresReview = false,
                SubmittedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                LastUpdated = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var createdApplication = await _unitOfWork.Applications.AddAsync(application);
            await _unitOfWork.SaveChangesAsync();

            // Lấy lại application cùng navigation properties
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
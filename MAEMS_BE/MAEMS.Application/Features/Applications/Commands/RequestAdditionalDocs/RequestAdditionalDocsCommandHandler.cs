using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.RequestAdditionalDocs;

public sealed class RequestAdditionalDocsCommandHandler : IRequestHandler<RequestAdditionalDocsCommand, BaseResponse<ApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public RequestAdditionalDocsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<BaseResponse<ApplicationDto>> Handle(RequestAdditionalDocsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.DocsNeed))
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid request",
                    new List<string> { "docs_need is required" });
            }

            // Validate officer exists
            var officer = await _unitOfWork.Users.GetByIdAsync(request.OfficerUserId);
            if (officer == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Officer not found",
                    new List<string> { $"User with ID {request.OfficerUserId} does not exist" });
            }

            var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
            if (application == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Application not found",
                    new List<string> { $"Application with ID {request.ApplicationId} does not exist" });
            }

            // Only allow from under_review
            if (!string.Equals(application.Status, "under_review", StringComparison.OrdinalIgnoreCase))
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid operation",
                    new List<string>
                    {
                        $"Additional documents can only be requested when application status is 'under_review'. Current status: '{application.Status}'."
                    });
            }

            if (!application.ApplicantId.HasValue)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid application",
                    new List<string> { "Application has no ApplicantId" });
            }

            var applicant = await _unitOfWork.Applicants.GetByIdAsync(application.ApplicantId.Value);
            if (applicant == null || !applicant.UserId.HasValue)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Applicant profile not found",
                    new List<string> { "Applicant user not found for this application" });
            }

            // Update application status
            application.Status = "document_required";
            application.LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _unitOfWork.Applications.UpdateAsync(application);

            // Create notification to applicant user
            // NOTE: When returning as JSON, double quotes are escaped as \". Using Vietnamese quotes avoids backslashes in clients.
            var message =
                $"Hồ sơ {application.ApplicationId} của bạn, được yêu cầu bổ sung các loại giấy tờ sau: {request.DocsNeed}. " +
                "Vui lòng bổ sung và tái nộp lại lần nữa";

            await _unitOfWork.Notifications.AddAsync(new Notification
            {
                RecipientUserId = applicant.UserId.Value,
                NotificationType = "Bổ sung giấy tờ",
                Message = message,
                IsRead = false,
                SentAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });

            // Send email status updated (after creating notification)
            var user = await _unitOfWork.Users.GetByIdAsync(applicant.UserId.Value);
            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                await _emailService.SendApplicationStatusUpdatedEmailAsync(
                    user.Email,
                    applicant.FullName,
                    application.ApplicationId);
            }

            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<ApplicationDto>(application);
            return BaseResponse<ApplicationDto>.SuccessResponse(dto, "Additional documents requested successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicationDto>.FailureResponse(
                "Error requesting additional documents",
                new List<string> { ex.Message });
        }
    }
}

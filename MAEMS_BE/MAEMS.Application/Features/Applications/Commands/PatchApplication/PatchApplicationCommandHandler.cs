using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.PatchApplication;

public class PatchApplicationCommandHandler : IRequestHandler<PatchApplicationCommand, BaseResponse<ApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;

    public PatchApplicationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _emailService = emailService;
    }

    public async Task<BaseResponse<ApplicationDto>> Handle(PatchApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Xác minh officer tồn tại trong hệ thống
            var officer = await _unitOfWork.Users.GetByIdAsync(request.OfficerUserId);
            if (officer == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Officer not found",
                    new List<string> { $"User with ID {request.OfficerUserId} does not exist" }
                );
            }

            // Lấy application theo id
            var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
            if (application == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Application not found",
                    new List<string> { $"Application with ID {request.ApplicationId} does not exist" }
                );
            }

            // Gán officer id từ JWT vào application
            application.AssignedOfficerId = request.OfficerUserId;

            // Chỉ cập nhật những trường được cung cấp (partial update)
            if (request.Status != null)
            {
                application.Status = request.Status;
            }

            if (request.RequiresReview.HasValue)
            {
                application.RequiresReview = request.RequiresReview.Value;
            }

            application.LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _unitOfWork.Applications.UpdateAsync(application);

            // Email applicant if status changed
            if (request.Status != null && application.ApplicantId.HasValue)
            {
                var applicant = await _unitOfWork.Applicants.GetByIdAsync(application.ApplicantId.Value);
                if (applicant?.UserId != null)
                {
                    var message = $"Hồ sơ {application.ApplicationId} của bạn đã được cập nhật, hãy vào coi chi tiết.";

                    await _unitOfWork.Notifications.AddAsync(new Notification
                    {
                        RecipientUserId = applicant.UserId.Value,
                        NotificationType = "Cập nhật trạng thái hồ sơ",
                        Message = message,
                        IsRead = false,
                        SentAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                    });

                    var user = await _unitOfWork.Users.GetByIdAsync(applicant.UserId.Value);
                    if (!string.IsNullOrWhiteSpace(user?.Email))
                    {
                        await _emailService.SendApplicationStatusUpdatedEmailAsync(
                            user.Email,
                            applicant.FullName,
                            application.ApplicationId);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<ApplicationDto>(application);

            return BaseResponse<ApplicationDto>.SuccessResponse(dto, "Application updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicationDto>.FailureResponse(
                "Error updating application",
                new List<string> { ex.Message }
            );
        }
    }
}

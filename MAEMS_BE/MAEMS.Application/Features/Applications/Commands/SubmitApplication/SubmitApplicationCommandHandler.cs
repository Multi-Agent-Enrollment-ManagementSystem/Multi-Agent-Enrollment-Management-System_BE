using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.SubmitApplication;

public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, BaseResponse<ApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDocumentVerificationAgent _verificationAgent;

    public SubmitApplicationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDocumentVerificationAgent verificationAgent)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _verificationAgent = verificationAgent;
    }

    public async Task<BaseResponse<ApplicationDto>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get applicant from userId (JWT)
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
            if (applicant == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Applicant profile not found",
                    new List<string> { "Please create applicant profile first" }
                );
            }

            // Get the application by id
            var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
            if (application == null)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Application not found",
                    new List<string> { $"Application with ID {request.ApplicationId} does not exist" }
                );
            }

            // Ensure the application belongs to the requesting applicant
            if (application.ApplicantId != applicant.ApplicantId)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Forbidden",
                    new List<string> { "You are not authorized to submit this application" }
                );
            }

            // Only allow submission if status is 'draft' or 'document_required'
            var currentStatus = application.Status ?? string.Empty;
            var canSubmit =
                string.Equals(currentStatus, "draft", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentStatus, "document_required", StringComparison.OrdinalIgnoreCase);

            if (!canSubmit)
            {
                return BaseResponse<ApplicationDto>.FailureResponse(
                    "Invalid operation",
                    new List<string>
                    {
                        $"Application cannot be submitted because its current status is '{application.Status}'. Only draft or document_required applications can be submitted."
                    }
                );
            }

            // Update status and timestamps
            application.Status = "submitted";
            application.SubmittedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            application.LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _unitOfWork.Applications.UpdateAsync(application);
            await _unitOfWork.SaveChangesAsync();

            var applicationDto = _mapper.Map<ApplicationDto>(application);

            // ── Fire-and-forget: Document Verification Agent ─────────────
            // Runs fully independently — does NOT block or affect this response.
            // Uses its own DI scope + DbContext internally.
            _verificationAgent.VerifyApplicationDocumentsAsync(request.ApplicationId);

            return BaseResponse<ApplicationDto>.SuccessResponse(applicationDto, "Application submitted successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicationDto>.FailureResponse(
                "Error submitting application",
                new List<string> { ex.Message }
            );
        }
    }
}

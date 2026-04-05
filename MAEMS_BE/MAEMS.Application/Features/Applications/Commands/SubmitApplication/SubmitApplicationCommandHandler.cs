using AutoMapper;
using MAEMS.Application.DTOs.Payment;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.SubmitApplication;

public class SubmitApplicationCommandHandler : IRequestHandler<SubmitApplicationCommand, BaseResponse<SubmitApplicationPaymentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IDocumentVerificationAgent _verificationAgent;
    private readonly IPaymentExpirationService _paymentExpirationService;

    private const string SEPAY_QR_BASE_URL = "https://qr.sepay.vn/img";
    private const string SEPAY_BANK = "TPBank";
    private const string SEPAY_ACCOUNT = "10001993956";
    private const string SEPAY_TEMPLATE = "compact";

    public SubmitApplicationCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IDocumentVerificationAgent verificationAgent,
        IPaymentExpirationService paymentExpirationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _verificationAgent = verificationAgent;
        _paymentExpirationService = paymentExpirationService;
    }

    public async Task<BaseResponse<SubmitApplicationPaymentDto>> Handle(SubmitApplicationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get applicant from userId (JWT)
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
            if (applicant == null)
            {
                return BaseResponse<SubmitApplicationPaymentDto>.FailureResponse(
                    "Applicant profile not found",
                    new List<string> { "Please create applicant profile first" }
                );
            }

            // Get the application by id
            var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
            if (application == null)
            {
                return BaseResponse<SubmitApplicationPaymentDto>.FailureResponse(
                    "Application not found",
                    new List<string> { $"Application with ID {request.ApplicationId} does not exist" }
                );
            }

            // Ensure the application belongs to the requesting applicant
            if (application.ApplicantId != applicant.ApplicantId)
            {
                return BaseResponse<SubmitApplicationPaymentDto>.FailureResponse(
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
                return BaseResponse<SubmitApplicationPaymentDto>.FailureResponse(
                    "Invalid operation",
                    new List<string>
                    {
                        $"Application cannot be submitted because its current status is '{application.Status}'. Only draft or document_required applications can be submitted."
                    }
                );
            }

            // ── Payment pre-check (before submit) ─────────────────────────
            var payments = await _unitOfWork.Payments.GetByApplicationIdAsync(request.ApplicationId);
            var hasPaid = payments.Any(p => string.Equals(p.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));

            if (!hasPaid)
            {
                var amount = 5000;
                var transactionId = $"NAP{request.UserId}{DateTime.UtcNow.Ticks}";

                var payment = await _unitOfWork.Payments.AddAsync(new Payment
                {
                    ApplicationId = request.ApplicationId,
                    ApplicantId = applicant.ApplicantId,
                    Amount = amount,
                    PaymentMethod = "Sepay",
                    TransactionId = transactionId,
                    ReferenceCode = null,
                    PaymentStatus = "pending",
                    PaidAt = null
                });

                await _unitOfWork.SaveChangesAsync();

                // Fire-and-forget: after 5 minutes, if still pending => mark as outdated
                _ = _paymentExpirationService.ScheduleExpirePendingPaymentAsync(
                    payment.PaymentId,
                    TimeSpan.FromMinutes(5),
                    CancellationToken.None);

                var depositDto = _mapper.Map<SubmitApplicationPaymentDto>(payment);
                depositDto.Url = $"{SEPAY_QR_BASE_URL}?bank={SEPAY_BANK}&acc={SEPAY_ACCOUNT}&template={SEPAY_TEMPLATE}&amount={amount}&des={transactionId}";

                return BaseResponse<SubmitApplicationPaymentDto>.SuccessResponse(depositDto, "Payment required");
            }

            // Update status and timestamps
            application.Status = "submitted";
            application.SubmittedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            application.LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _unitOfWork.Applications.UpdateAsync(application);
            await _unitOfWork.SaveChangesAsync();

            // ── Fire-and-forget: Document Verification Agent ─────────────
            // Runs fully independently — does NOT block or affect this response.
            // Uses its own DI scope + DbContext internally.
            _verificationAgent.VerifyApplicationDocumentsAsync(request.ApplicationId);

            return BaseResponse<SubmitApplicationPaymentDto>.SuccessResponse(
                new SubmitApplicationPaymentDto(),
                "Application submitted successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<SubmitApplicationPaymentDto>.FailureResponse(
                "Error submitting application",
                new List<string> { ex.Message }
            );
        }
    }
}

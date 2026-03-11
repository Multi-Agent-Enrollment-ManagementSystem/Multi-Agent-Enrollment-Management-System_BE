using AutoMapper;
using MAEMS.Application.DTOs.Document;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using IFileStorageService = MAEMS.Application.Interfaces.IFileStorageService;

namespace MAEMS.Application.Features.Documents.Commands.UploadApplicantDocument;

public class UploadApplicantDocumentCommandHandler : IRequestHandler<UploadApplicantDocumentCommand, BaseResponse<DocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentIntakeAgent _documentIntakeAgent;
    private readonly IMapper _mapper;
    private readonly ILogger<UploadApplicantDocumentCommandHandler> _logger;

    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadApplicantDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IDocumentIntakeAgent documentIntakeAgent,
        IMapper mapper,
        ILogger<UploadApplicantDocumentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _documentIntakeAgent = documentIntakeAgent;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<DocumentDto>> Handle(UploadApplicantDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate applicant exists
            var applicant = await _unitOfWork.Applicants.GetByIdAsync(request.ApplicantId);
            if (applicant == null)
            {
                return BaseResponse<DocumentDto>.FailureResponse(
                    "Applicant not found",
                    new List<string> { $"Applicant with ID {request.ApplicantId} does not exist" }
                );
            }

            if (applicant.UserId != request.UserId)
            {
                return BaseResponse<DocumentDto>.FailureResponse(
                    "Forbidden",
                    new List<string> { "User does not have permission to upload document for this applicant" }
                );
            }

            // Validate file format & size
            var validationResult = ValidateFile(request.File);
            if (!validationResult.IsValid)
            {
                return BaseResponse<DocumentDto>.FailureResponse(
                    "Invalid file",
                    validationResult.Errors
                );
            }

            // Quality check
            _logger.LogInformation(
                "Sending file '{FileName}' to DocumentIntakeAgent for quality check (ApplicantId={ApplicantId})",
                request.File.FileName, request.ApplicantId);

            var qualityResult = await _documentIntakeAgent.CheckDocumentQualityAsync(
                request.File, cancellationToken);

            if (!qualityResult.PassedQualityCheck)
            {
                _logger.LogWarning(
                    "Document quality check failed for '{FileName}' (ApplicantId={ApplicantId}). Issues: {Issues}",
                    request.File.FileName, request.ApplicantId,
                    string.Join("; ", qualityResult.Issues));

                var errors = qualityResult.Issues.Count > 0
                    ? qualityResult.Issues
                    : new List<string> { "Document did not pass quality check" };

                return BaseResponse<DocumentDto>.FailureResponse(
                    "Document quality check failed",
                    errors
                );
            }

            _logger.LogInformation(
                "Document quality check passed for '{FileName}'. Type='{DocumentType}', Confidence={Confidence:P0}",
                request.File.FileName, qualityResult.DocumentType, qualityResult.Confidence);

            // Upload file
            string downloadUrl;
            using (var stream = request.File.OpenReadStream())
            {
                downloadUrl = await _fileStorageService.UploadFileAsync(
                    stream,
                    request.File.FileName,
                    $"applicants/{request.ApplicantId}/documents"
                );
            }

            // Create document
            var newDocument = new MAEMS.Domain.Entities.Document
            {
                ApplicantId = request.ApplicantId,
                FilePath = downloadUrl,
                UploadedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                FileName = request.File.FileName,
                FileFormat = Path.GetExtension(request.File.FileName),
                DocumentType = qualityResult.DocumentType,
                VerificationResult = "pending",
                VerificationDetails = null
            };

            var createdDocument = await _unitOfWork.Documents.AddAsync(newDocument);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = _mapper.Map<DocumentDto>(createdDocument);

            return BaseResponse<DocumentDto>.SuccessResponse(responseDto, "Document uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for applicant {ApplicantId}", request.ApplicantId);
            return BaseResponse<DocumentDto>.FailureResponse(
                "Error uploading document",
                new List<string> { ex.Message }
            );
        }
    }

    private ValidationResult ValidateFile(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var errors = new List<string>();

        if (file == null || file.Length == 0)
        {
            errors.Add("File is required");
            return new ValidationResult { IsValid = false, Errors = errors };
        }

        if (file.Length > _maxFileSize)
        {
            errors.Add($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            errors.Add($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
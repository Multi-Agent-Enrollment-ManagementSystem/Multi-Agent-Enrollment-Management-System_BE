using AutoMapper;
using MAEMS.Application.DTOs.Document;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using IFileStorageService = MAEMS.Application.Interfaces.IFileStorageService;

namespace MAEMS.Application.Features.Documents.Commands.UploadDocument;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, BaseResponse<DocumentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<UploadDocumentCommandHandler> _logger;
    
    private readonly string[] _allowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };
    private const long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public UploadDocumentCommandHandler(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<UploadDocumentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<BaseResponse<DocumentDto>> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate application exists
            var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
            if (application == null)
            {
                return BaseResponse<DocumentDto>.FailureResponse(
                    "Application not found",
                    new List<string> { $"Application with ID {request.ApplicationId} does not exist" }
                );
            }

            // Validate file
            var validationResult = ValidateFile(request.File);
            if (!validationResult.IsValid)
            {
                return BaseResponse<DocumentDto>.FailureResponse(
                    "Invalid file",
                    validationResult.Errors
                );
            }

            // Upload file lên Firebase Storage — trả về public download URL
            string downloadUrl;
            using (var stream = request.File.OpenReadStream())
            {
                downloadUrl = await _fileStorageService.UploadFileAsync(
                    stream,
                    request.File.FileName,
                    $"applications/{request.ApplicationId}/documents"
                );
            }

            // Tạo document mới — FilePath lưu public download URL
            var newDocument = new Domain.Entities.Document
            {
                ApplicationId = request.ApplicationId,
                FilePath = downloadUrl,
                UploadedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified),
                FileName = request.File.FileName,
                FileFormat = Path.GetExtension(request.File.FileName),
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
            _logger.LogError(ex, "Error uploading document for application {ApplicationId}", request.ApplicationId);
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
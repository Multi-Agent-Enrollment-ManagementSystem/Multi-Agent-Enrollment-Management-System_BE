using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace MAEMS.Application.Features.Documents.Commands.UploadApplicantDocument;

public class UploadApplicantDocumentCommand : IRequest<BaseResponse<DocumentDto>>
{
    public int ApplicantId { get; set; }
    public IFormFile File { get; set; } = null!;
    public int UserId { get; set; } // Add this line
}
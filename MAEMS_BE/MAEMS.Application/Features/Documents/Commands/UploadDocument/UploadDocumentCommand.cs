using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace MAEMS.Application.Features.Documents.Commands.UploadDocument;

public class UploadDocumentCommand : IRequest<BaseResponse<DocumentDto>>
{
    public int ApplicationId { get; set; }
    public IFormFile File { get; set; } = null!;
}
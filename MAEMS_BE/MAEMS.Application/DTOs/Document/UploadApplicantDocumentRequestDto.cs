using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MAEMS.Application.DTOs.Document;

public class UploadApplicantDocumentRequestDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
}
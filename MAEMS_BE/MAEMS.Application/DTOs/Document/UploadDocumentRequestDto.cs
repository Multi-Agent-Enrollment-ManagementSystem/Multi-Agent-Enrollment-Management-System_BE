using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MAEMS.Application.DTOs.Document;

public class UploadDocumentRequestDto
{
    [Required]
    public string DocumentType { get; set; } = string.Empty;
    
    [Required]
    public IFormFile File { get; set; } = null!;
}
using Microsoft.AspNetCore.Http;

namespace MAEMS.Application.DTOs.MajorAdvisor;

/// <summary>
/// Request DTO for analyzing academic document
/// </summary>
public sealed class AnalyzeDocumentRequestDto
{
    /// <summary>
    /// Academic document file (học bạ THPT or kết quả thi ĐGNL)
    /// Supported formats: .jpg, .jpeg, .png, .pdf
    /// Max size: 20MB
    /// </summary>
    public IFormFile File { get; set; } = null!;
}

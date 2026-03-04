using Microsoft.AspNetCore.Http;

namespace MAEMS.Application.Interfaces;

/// <summary>
/// Application Intake Agent — kiểm tra chất lượng và nhận dạng loại tài liệu bằng LLM.
/// </summary>
public interface IDocumentIntakeAgent
{
    /// <summary>
    /// Gửi file tài liệu cho LLM để kiểm tra chất lượng và nhận dạng loại tài liệu.
    /// </summary>
    /// <param name="file">File tải lên từ người dùng.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Kết quả kiểm tra chất lượng từ LLM.</returns>
    Task<DocumentQualityCheckResult> CheckDocumentQualityAsync(IFormFile file, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kết quả kiểm tra chất lượng tài liệu trả về từ LLM.
/// </summary>
public class DocumentQualityCheckResult
{
    public string DocumentType { get; set; } = "other";
    public bool PassedQualityCheck { get; set; }
    public DocumentQuality Quality { get; set; } = new();
    public double Confidence { get; set; }
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Chi tiết kiểm tra chất lượng từng tiêu chí.
/// </summary>
public class DocumentQuality
{
    public bool IsReadable { get; set; }
    public bool IsUnobscured { get; set; }
    public bool IsUnblurred { get; set; }
    public bool IsComplete { get; set; }
    public bool IsUnedited { get; set; }
}

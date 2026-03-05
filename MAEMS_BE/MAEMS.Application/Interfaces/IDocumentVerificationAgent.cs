namespace MAEMS.Application.Interfaces;

/// <summary>
/// Document Verification Agent — nhận thông tin applicant và tất cả documents của một application,
/// kiểm tra xem thông tin trên các document có khớp với nhau và với hồ sơ applicant không.
/// Kết quả được lưu thẳng vào <c>Document.VerificationResult</c> và <c>Document.VerificationDetails</c>.
/// </summary>
public interface IDocumentVerificationAgent
{
    /// <summary>
    /// Chạy verification cho toàn bộ documents của một application.
    /// Fire-and-forget: gọi không cần await từ caller.
    /// </summary>
    /// <param name="applicationId">ID của application cần verify.</param>
    Task VerifyApplicationDocumentsAsync(int applicationId);
}

/// <summary>
/// Kết quả verification cho một document đơn lẻ (được map từ LLM JSON response).
/// </summary>
public class DocumentVerificationResult
{
    /// <summary>"verified" hoặc "rejected"</summary>
    public string Result { get; set; } = "rejected";

    /// <summary>Lý do từ chối — chỉ có giá trị khi <see cref="Result"/> = "rejected".</summary>
    public string? Details { get; set; }
}

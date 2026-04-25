namespace MAEMS.Application.Interfaces;

/// <summary>
/// Eligibility Evaluation Agent — kiểm tra hồ sơ ứng viên có đủ điều kiện nộp theo
/// admission type hay không, đồng thời nhận xét sơ bộ về chất lượng hồ sơ.
/// Kết quả được lưu vào <c>Application.Notes</c> và <c>Application.RequiresReview</c>.
/// </summary>
public interface IEligibilityEvaluationAgent
{
    /// <summary>
    /// Chạy đánh giá điều kiện cho một application sau khi verification hoàn tất.
    /// Được gọi nội bộ bởi DocumentVerificationAgent — không cần await từ bên ngoài.
    /// </summary>
    /// <param name="applicationId">ID của application cần đánh giá.</param>
    /// <param name="verificationNotes">
    /// Danh sách details từ các document bị rejected, được nối vào Notes cuối cùng.
    /// </param>
    Task EvaluateAsync(int applicationId, List<string> verificationNotes);
}

/// <summary>
/// Kết quả đánh giá điều kiện từ LLM.
/// </summary>
public class EligibilityEvaluationResult
{
    /// <summary>"passed" hoặc "rejected"</summary>
    public string Result { get; set; } = "rejected";

    /// <summary>Level ưu tiên nếu passed (VD: Normal, Good, Great, Excellent)</summary>
    public string? Level { get; set; }

    /// <summary>Nhận xét / lý do từ LLM.</summary>
    public string? Details { get; set; }
}

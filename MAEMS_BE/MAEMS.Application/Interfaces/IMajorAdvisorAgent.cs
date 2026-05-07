using MAEMS.Application.DTOs.MajorAdvisor;
using Microsoft.AspNetCore.Http;

namespace MAEMS.Application.Interfaces;

/// <summary>
/// Major Advisor Agent - Analyzes academic transcripts or competency test results
/// and recommends suitable university majors. Public service (no authentication required).
/// </summary>
public interface IMajorAdvisorAgent
{
    /// <summary>
    /// Analyzes a single academic document (transcript or competency test)
    /// and provides major recommendations. Logs the analysis to AgentLog for QA review.
    /// </summary>
    /// <param name="file">Học bạ THPT or Kết quả thi ĐGNL (image/PDF)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document analysis and major recommendations</returns>
    Task<MajorAdvisorResult> AnalyzeAndRecommendAsync(
        IFormFile file,
        CancellationToken cancellationToken = default);
}

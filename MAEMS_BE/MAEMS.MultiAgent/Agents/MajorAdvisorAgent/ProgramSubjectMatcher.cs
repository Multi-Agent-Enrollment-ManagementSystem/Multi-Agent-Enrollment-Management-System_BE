namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Helper class to calculate match score between student subjects and program requirements
/// </summary>
public static class ProgramSubjectMatcher
{
    // Subject categories
    public enum SubjectCategory
    {
        STEM,           // Toán, Lý, Hóa, Sinh, Tin
        Social,         // Văn, Sử, Địa, GDCD
        Language,       // Anh, ngoại ngữ
        Business,       // Hybrid: Toán + Anh + Văn
        Medical         // Hóa, Sinh, Toán
    }

    /// <summary>
    /// Calculate match score (0-100) based on student scores and program category
    /// </summary>
    public static int CalculateMatchScore(
        Dictionary<string, decimal> studentScores,
        string programName,
        decimal? schoolRankScore = null,
        int? schoolRank = null,
        decimal? competencyTotalScore = null)
    {
        var category = ClassifyProgram(programName);
        var requiredSubjects = GetRequiredSubjects(category);

        // Extract relevant subject scores
        var relevantScores = new List<decimal>();
        foreach (var subject in requiredSubjects)
        {
            if (studentScores.TryGetValue(subject, out var score))
            {
                relevantScores.Add(score);
            }
        }

        // If no relevant scores found, use fallback
        if (relevantScores.Count == 0)
        {
            return CalculateFallbackScore(studentScores, schoolRankScore, schoolRank, competencyTotalScore);
        }

        // Calculate base score from relevant subject average (scale 0-10 to 0-100)
        var avgScore = relevantScores.Average();
        var baseScore = (avgScore / 10.0m) * 100;

        // Apply subject alignment multiplier (reward strong match between student strengths and program requirements)
        var alignmentMultiplier = CalculateSubjectAlignmentMultiplier(studentScores, requiredSubjects, avgScore);
        baseScore *= alignmentMultiplier;

        // Apply performance-based bonus depending on document type
        baseScore += CalculatePerformanceBonus(avgScore, schoolRank, schoolRankScore, competencyTotalScore);

        // Cap at 100
        return (int)Math.Min(100, Math.Round(baseScore));
    }

    /// <summary>
    /// Calculate alignment multiplier based on how well student's strongest subjects match program requirements
    /// </summary>
    private static decimal CalculateSubjectAlignmentMultiplier(
        Dictionary<string, decimal> allScores,
        List<string> requiredSubjects,
        decimal requiredAverage)
    {
        if (!allScores.Any()) return 1.0m;

        // Find student's top 3 subjects
        var topSubjects = allScores
            .OrderByDescending(s => s.Value)
            .Take(3)
            .Select(s => s.Key)
            .ToHashSet();

        // Calculate overlap between top subjects and required subjects
        var matchCount = requiredSubjects.Count(s => topSubjects.Contains(s));
        var matchRatio = (decimal)matchCount / Math.Min(3, requiredSubjects.Count);

        // Also compare student's average in required subjects vs overall average
        var overallAverage = allScores.Values.Average();
        var strengthRatio = requiredAverage / overallAverage;

        // Multiplier formula:
        // - Perfect match (student's strengths = program requirements): 1.15x
        // - Good match (2/3 overlap): 1.10x
        // - Fair match (1/3 overlap): 1.05x
        // - Poor match (no overlap but still decent scores): 1.00x
        // - Weakness match (required subjects are weak points): 0.90-0.95x

        if (strengthRatio >= 1.15m && matchRatio >= 0.67m)
            return 1.15m; // Excellent alignment: strong scores in required subjects

        if (strengthRatio >= 1.10m && matchRatio >= 0.5m)
            return 1.10m; // Good alignment

        if (strengthRatio >= 1.05m || matchRatio >= 0.33m)
            return 1.05m; // Fair alignment

        if (strengthRatio >= 0.95m)
            return 1.00m; // Neutral (required subjects = average)

        // Penalty for programs requiring subjects student is weak at
        return 0.92m;
    }

    /// <summary>
    /// Calculate performance bonus based on document type and scores
    /// </summary>
    private static decimal CalculatePerformanceBonus(
        decimal avgScore,
        int? schoolRank = null,
        decimal? schoolRankScore = null,
        decimal? competencyTotalScore = null)
    {
        // Priority 1: SchoolRank bonus (if available)
        if (schoolRank.HasValue)
        {
            return CalculateSchoolRankBonus(schoolRank.Value);
        }

        // Priority 2: ĐGNL bonus (if available)
        if (competencyTotalScore.HasValue)
        {
            return CalculateCompetencyBonus(competencyTotalScore.Value);
        }

        // Priority 3: Transcript excellence bonus (based on average)
        return CalculateTranscriptBonus(avgScore);
    }

    /// <summary>
    /// Calculate transcript bonus based on average grade (học bạ)
    /// </summary>
    private static decimal CalculateTranscriptBonus(decimal avgScore)
    {
        if (avgScore >= 9.5m) return 15;  // Xuất sắc (9.5-10)
        if (avgScore >= 9.0m) return 12;  // Giỏi cao (9.0-9.5)
        if (avgScore >= 8.5m) return 10;  // Giỏi (8.5-9.0)
        if (avgScore >= 8.0m) return 7;   // Khá giỏi (8.0-8.5)
        if (avgScore >= 7.5m) return 5;   // Khá (7.5-8.0)
        if (avgScore >= 7.0m) return 3;   // Trung bình khá (7.0-7.5)
        return 1;                          // Trung bình (< 7.0)
    }

    /// <summary>
    /// Calculate ĐGNL bonus based on total score
    /// </summary>
    private static decimal CalculateCompetencyBonus(decimal totalScore)
    {
        if (totalScore >= 900) return 15;  // Xuất sắc (900-1200)
        if (totalScore >= 850) return 12;  // Giỏi cao (850-900)
        if (totalScore >= 800) return 10;  // Giỏi (800-850)
        if (totalScore >= 750) return 7;   // Khá giỏi (750-800)
        if (totalScore >= 700) return 5;   // Khá (700-750)
        if (totalScore >= 650) return 3;   // Trung bình khá (650-700)
        return 1;                           // Trung bình (< 650)
    }

    /// <summary>
    /// Classify program into subject category based on name
    /// </summary>
    private static SubjectCategory ClassifyProgram(string programName)
    {
        var name = programName.ToLowerInvariant();

        // STEM: IT, Engineering, Computer Science
        if (name.Contains("công nghệ thông tin") || name.Contains("khoa học máy tính") ||
            name.Contains("kỹ thuật") || name.Contains("điện") || name.Contains("cơ khí") ||
            name.Contains("xây dựng") || name.Contains("công nghệ") || name.Contains("trí tuệ nhân tạo"))
            return SubjectCategory.STEM;

        // Medical: Medicine, Pharmacy, Biology
        if (name.Contains("y ") || name.Contains("dược") || name.Contains("sinh học ứng dụng") ||
            name.Contains("điều dưỡng") || name.Contains("y tế"))
            return SubjectCategory.Medical;

        // Language: Foreign languages, Translation
        if (name.Contains("ngôn ngữ") || name.Contains("tiếng") || name.Contains("biên phiên dịch"))
            return SubjectCategory.Language;

        // Business: Economics, Management, Marketing (hybrid: Toán + Anh + Văn)
        if (name.Contains("quản trị") || name.Contains("kinh tế") || name.Contains("marketing") ||
            name.Contains("kinh doanh") || name.Contains("tài chính") || name.Contains("kế toán") ||
            name.Contains("thương mại") || name.Contains("du lịch") || name.Contains("logistics"))
            return SubjectCategory.Business;

        // Social: Humanities, Social Sciences
        if (name.Contains("xã hội") || name.Contains("chính trị") || name.Contains("văn hóa") ||
            name.Contains("báo chí") || name.Contains("truyền thông") || name.Contains("quan hệ"))
            return SubjectCategory.Social;

        // Default: STEM (most FPT programs are tech-oriented)
        return SubjectCategory.STEM;
    }

    /// <summary>
    /// Get required subjects for each program category
    /// </summary>
    private static List<string> GetRequiredSubjects(SubjectCategory category)
    {
        return category switch
        {
            SubjectCategory.STEM => new List<string> { "Toán", "Lý", "Hóa", "Tin" },
            SubjectCategory.Medical => new List<string> { "Hóa", "Sinh", "Toán" },
            SubjectCategory.Language => new List<string> { "Anh", "Văn", "Sử" },
            SubjectCategory.Business => new List<string> { "Toán", "Anh", "Văn" },
            SubjectCategory.Social => new List<string> { "Văn", "Sử", "Địa", "GDCD" },
            _ => new List<string> { "Toán", "Anh", "Văn" }
        };
    }

    /// <summary>
    /// Calculate SchoolRank bonus points (top 10 highest, gradually decreasing to top 50)
    /// </summary>
    private static decimal CalculateSchoolRankBonus(int rank)
    {
        if (rank <= 10) return 15;   // Top 10: Highest bonus
        if (rank <= 20) return 12;   // Top 20: Excellent
        if (rank <= 30) return 10;   // Top 30: Great
        if (rank <= 50) return 7;    // Top 50: Good
        if (rank <= 100) return 5;   // Top 100: Fair
        if (rank <= 200) return 3;   // Top 200: Basic
        return 1;                     // Beyond 200: Minimal bonus
    }

    /// <summary>
    /// Fallback score calculation when no relevant subject scores available
    /// </summary>
    private static int CalculateFallbackScore(
        Dictionary<string, decimal> allScores,
        decimal? schoolRankScore,
        int? schoolRank,
        decimal? competencyTotalScore = null)
    {
        // Use overall average if available
        if (allScores.Any())
        {
            var avg = allScores.Values.Average();
            var baseScore = (avg / 10.0m) * 100;

            // Apply appropriate bonus
            baseScore += CalculatePerformanceBonus(avg, schoolRank, schoolRankScore, competencyTotalScore);

            return (int)Math.Min(100, Math.Round(baseScore));
        }

        // Last resort: use SchoolRank score
        if (schoolRankScore.HasValue)
        {
            var normalized = (schoolRankScore.Value / 30.0m) * 100; // Normalize 30-point scale
            return (int)Math.Min(100, Math.Round(normalized));
        }

        // Or use competency total score
        if (competencyTotalScore.HasValue)
        {
            var normalized = (competencyTotalScore.Value / 1200.0m) * 100; // Normalize 1200-point scale
            return (int)Math.Min(100, Math.Round(normalized));
        }

        return 70; // Default score
    }

    /// <summary>
    /// Build student score dictionary from transcript data
    /// </summary>
    public static Dictionary<string, decimal> BuildScoreDictionary(
        MAEMS.Application.DTOs.MajorAdvisor.TranscriptData transcript)
    {
        var scores = new Dictionary<string, decimal>();

        // Prioritize Grade 12 scores, fallback to Grade 11
        if (transcript.Grade12_Toan.HasValue && transcript.Grade12_Toan > 0)
            scores["Toán"] = transcript.Grade12_Toan.Value;
        else if (transcript.Grade11_Toan.HasValue && transcript.Grade11_Toan > 0)
            scores["Toán"] = transcript.Grade11_Toan.Value;

        if (transcript.Grade12_NguVan.HasValue && transcript.Grade12_NguVan > 0)
            scores["Văn"] = transcript.Grade12_NguVan.Value;
        else if (transcript.Grade11_NguVan.HasValue && transcript.Grade11_NguVan > 0)
            scores["Văn"] = transcript.Grade11_NguVan.Value;

        if (transcript.Grade12_NgoaiNgu.HasValue && transcript.Grade12_NgoaiNgu > 0)
            scores["Anh"] = transcript.Grade12_NgoaiNgu.Value;
        else if (transcript.Grade11_NgoaiNgu.HasValue && transcript.Grade11_NgoaiNgu > 0)
            scores["Anh"] = transcript.Grade11_NgoaiNgu.Value;

        if (transcript.Grade12_VatLy.HasValue && transcript.Grade12_VatLy > 0)
            scores["Lý"] = transcript.Grade12_VatLy.Value;
        else if (transcript.Grade11_VatLy.HasValue && transcript.Grade11_VatLy > 0)
            scores["Lý"] = transcript.Grade11_VatLy.Value;

        if (transcript.Grade12_HoaHoc.HasValue && transcript.Grade12_HoaHoc > 0)
            scores["Hóa"] = transcript.Grade12_HoaHoc.Value;
        else if (transcript.Grade11_HoaHoc.HasValue && transcript.Grade11_HoaHoc > 0)
            scores["Hóa"] = transcript.Grade11_HoaHoc.Value;

        if (transcript.Grade12_SinhHoc.HasValue && transcript.Grade12_SinhHoc > 0)
            scores["Sinh"] = transcript.Grade12_SinhHoc.Value;
        else if (transcript.Grade11_SinhHoc.HasValue && transcript.Grade11_SinhHoc > 0)
            scores["Sinh"] = transcript.Grade11_SinhHoc.Value;

        if (transcript.Grade12_LichSu.HasValue && transcript.Grade12_LichSu > 0)
            scores["Sử"] = transcript.Grade12_LichSu.Value;
        else if (transcript.Grade11_LichSu.HasValue && transcript.Grade11_LichSu > 0)
            scores["Sử"] = transcript.Grade11_LichSu.Value;

        if (transcript.Grade12_DiaLy.HasValue && transcript.Grade12_DiaLy > 0)
            scores["Địa"] = transcript.Grade12_DiaLy.Value;
        else if (transcript.Grade11_DiaLy.HasValue && transcript.Grade11_DiaLy > 0)
            scores["Địa"] = transcript.Grade11_DiaLy.Value;

        if (transcript.Grade12_GDCD.HasValue && transcript.Grade12_GDCD > 0)
            scores["GDCD"] = transcript.Grade12_GDCD.Value;
        else if (transcript.Grade11_GDCD.HasValue && transcript.Grade11_GDCD > 0)
            scores["GDCD"] = transcript.Grade11_GDCD.Value;

        // Note: Tin học is not typically in transcript, but included in model
        return scores;
    }

    /// <summary>
    /// Build score dictionary from ĐGNL competency test
    /// </summary>
    public static Dictionary<string, decimal> BuildScoreDictionary(
        MAEMS.Application.DTOs.MajorAdvisor.CompetencyData competency)
    {
        var scores = new Dictionary<string, decimal>();

        // Map ĐGNL components to subjects (normalized to 0-10 scale)
        if (competency.ToanHoc.HasValue)
            scores["Toán"] = (competency.ToanHoc.Value / 300m) * 10;

        if (competency.TuDuyKhoaHoc.HasValue)
            scores["Lý"] = (competency.TuDuyKhoaHoc.Value / 300m) * 10; // Science reasoning ~ Physics

        if (competency.TiengViet.HasValue)
            scores["Văn"] = (competency.TiengViet.Value / 300m) * 10;

        if (competency.TiengAnh.HasValue)
            scores["Anh"] = (competency.TiengAnh.Value / 300m) * 10;

        return scores;
    }
}

namespace MAEMS.Application.DTOs.MajorAdvisor;

/// <summary>
/// Document type detection result
/// </summary>
public sealed class DocumentTypeResult
{
    public DocumentType Type { get; set; }
    public double Confidence { get; set; }
}

public enum DocumentType
{
    Unknown,
    Transcript,      // Học bạ THPT
    CompetencyTest   // Điểm ĐGNL
}

/// <summary>
/// Extracted scores from documents
/// </summary>
public sealed class ExtractedScores
{
    // From Transcript (Học bạ)
    public TranscriptData? Transcript { get; set; }

    // From Competency Test (ĐGNL)
    public CompetencyData? Competency { get; set; }
}

public sealed class TranscriptData
{
    // Grade 11 scores
    public decimal? Grade11_Toan { get; set; }
    public decimal? Grade11_NguVan { get; set; }
    public decimal? Grade11_NgoaiNgu { get; set; }
    public decimal? Grade11_VatLy { get; set; }
    public decimal? Grade11_HoaHoc { get; set; }
    public decimal? Grade11_SinhHoc { get; set; }
    public decimal? Grade11_LichSu { get; set; }
    public decimal? Grade11_DiaLy { get; set; }
    public decimal? Grade11_GDCD { get; set; }

    // Grade 12 scores
    public decimal? Grade12_Toan { get; set; }
    public decimal? Grade12_NguVan { get; set; }
    public decimal? Grade12_NgoaiNgu { get; set; }
    public decimal? Grade12_VatLy { get; set; }
    public decimal? Grade12_HoaHoc { get; set; }
    public decimal? Grade12_SinhHoc { get; set; }
    public decimal? Grade12_LichSu { get; set; }
    public decimal? Grade12_DiaLy { get; set; }
    public decimal? Grade12_GDCD { get; set; }

    public decimal? AverageGpa { get; set; }
}

public sealed class CompetencyData
{
    public decimal? TotalScore { get; set; }      // Điểm tổng (e.g., 876/1200)
    public decimal? TiengViet { get; set; }       // Tiếng Việt (e.g., 258/300)
    public decimal? TiengAnh { get; set; }        // Tiếng Anh (e.g., 191/300)
    public decimal? ToanHoc { get; set; }         // Toán học (e.g., 203/300)
    public decimal? TuDuyKhoaHoc { get; set; }    // Tư duy khoa học (e.g., 224/300)
    public string? PercentileRange { get; set; }  // e.g., "801-900"
}

/// <summary>
/// Final result returned to user
/// </summary>
public sealed class MajorAdvisorResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DocumentType DetectedDocumentType { get; set; }
    public ExtractedScores? Scores { get; set; }
    public List<MajorRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Single major recommendation
/// </summary>
public sealed class MajorRecommendation
{
    public string MajorCode { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public int MatchScore { get; set; } // 0-100
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
    public string AdmissionMethod { get; set; } = string.Empty; // "Xét học bạ" | "Xét ĐGNL"
}

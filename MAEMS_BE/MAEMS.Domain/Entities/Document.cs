namespace MAEMS.Domain.Entities;

public class Document
{
    public int DocumentId { get; set; }
    public int? ApplicantId { get; set; } // Chỉ có ApplicantId, không có ApplicationId
    public string? DocumentType { get; set; }
    public string? FilePath { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? FileName { get; set; }
    public string? FileFormat { get; set; }
    public string? VerificationResult { get; set; }
    public string? VerificationDetails { get; set; }
}
namespace MAEMS.Domain.Entities;

public class Document
{
    public int DocumentId { get; set; }
    public int? ApplicationId { get; set; }
    public string? DocumentType { get; set; }
    public string? FilePath { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? FileName { get; set; }
    public string? FileFormat { get; set; }
    public string? VerificationResult { get; set; }
    public string? VerificationDetails { get; set; }
}
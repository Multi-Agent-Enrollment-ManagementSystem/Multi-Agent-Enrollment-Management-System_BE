using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<IEnumerable<Document>> GetByApplicantIdAsync(int applicantId); // Chỉ có ApplicantId
    Task<IEnumerable<Document>> GetByDocumentTypeAsync(string documentType);
    Task<Document?> GetByApplicantIdAndTypeAsync(int applicantId, string documentType);
}
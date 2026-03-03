using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IDocumentRepository : IGenericRepository<Document>
{
    Task<IEnumerable<Document>> GetByApplicationIdAsync(int applicationId);
    Task<IEnumerable<Document>> GetByDocumentTypeAsync(string documentType);
    Task<Document?> GetByApplicationIdAndTypeAsync(int applicationId, string documentType);
}
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using DomainDocument = MAEMS.Domain.Entities.Document;
using InfraDocument = MAEMS.Infrastructure.Models.Document;

namespace MAEMS.Infrastructure.Repositories;

public class DocumentRepository : BaseRepository, IDocumentRepository
{
    public DocumentRepository(postgresContext context) : base(context) { }

    public async Task<DomainDocument?> GetByIdAsync(int id)
    {
        var infraDocument = await _context.Documents.FindAsync(id);
        return infraDocument == null ? null : MapToDomain(infraDocument);
    }

    public async Task<IEnumerable<DomainDocument>> GetAllAsync()
    {
        var infraDocuments = await _context.Documents.ToListAsync();
        return infraDocuments.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainDocument>> FindAsync(Expression<Func<DomainDocument, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainDocument> AddAsync(DomainDocument entity)
    {
        var infraDocument = new InfraDocument
        {
            ApplicantId = entity.ApplicantId,
            DocumentType = entity.DocumentType,
            FilePath = entity.FilePath,
            UploadedAt = entity.UploadedAt,
            FileName = entity.FileName,
            FileFormat = entity.FileFormat,
            VerificationResult = entity.VerificationResult,
            VerificationDetails = entity.VerificationDetails
        };

        await _context.Documents.AddAsync(infraDocument);
        entity.DocumentId = infraDocument.DocumentId;
        return entity;
    }

    public async Task UpdateAsync(DomainDocument entity)
    {
        var infraDocument = await _context.Documents.FindAsync(entity.DocumentId);
        if (infraDocument != null)
        {
            infraDocument.ApplicantId = entity.ApplicantId;
            infraDocument.DocumentType = entity.DocumentType;
            infraDocument.FilePath = entity.FilePath;
            infraDocument.UploadedAt = entity.UploadedAt;
            infraDocument.FileName = entity.FileName;
            infraDocument.FileFormat = entity.FileFormat;
            infraDocument.VerificationResult = entity.VerificationResult;
            infraDocument.VerificationDetails = entity.VerificationDetails;

            _context.Documents.Update(infraDocument);
        }
    }

    public async Task DeleteAsync(DomainDocument entity)
    {
        var infraDocument = await _context.Documents.FindAsync(entity.DocumentId);
        if (infraDocument != null)
        {
            _context.Documents.Remove(infraDocument);
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainDocument, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    public async Task<IEnumerable<DomainDocument>> GetByApplicantIdAsync(int applicantId)
    {
        var infraDocs = await _context.Documents
            .Where(d => d.ApplicantId == applicantId)
            .ToListAsync();

        return infraDocs.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainDocument>> GetByDocumentTypeAsync(string documentType)
    {
        var infraDocuments = await _context.Documents
            .Where(d => d.DocumentType == documentType)
            .ToListAsync();

        return infraDocuments.Select(MapToDomain);
    }

    public async Task<DomainDocument?> GetByApplicantIdAndTypeAsync(int applicantId, string documentType)
    {
        var infraDocument = await _context.Documents
            .FirstOrDefaultAsync(d => d.ApplicantId == applicantId && d.DocumentType == documentType);

        return infraDocument == null ? null : MapToDomain(infraDocument);
    }

    private static DomainDocument MapToDomain(InfraDocument infraDocument)
    {
        return new DomainDocument
        {
            DocumentId = infraDocument.DocumentId,
            ApplicantId = infraDocument.ApplicantId,
            DocumentType = infraDocument.DocumentType,
            FilePath = infraDocument.FilePath,
            UploadedAt = infraDocument.UploadedAt,
            FileName = infraDocument.FileName,
            FileFormat = infraDocument.FileFormat,
            VerificationResult = infraDocument.VerificationResult,
            VerificationDetails = infraDocument.VerificationDetails
        };
    }
}
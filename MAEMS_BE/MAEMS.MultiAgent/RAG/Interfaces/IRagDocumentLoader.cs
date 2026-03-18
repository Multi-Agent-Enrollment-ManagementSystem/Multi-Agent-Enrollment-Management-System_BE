using MAEMS.MultiAgent.RAG.Models;

namespace MAEMS.MultiAgent.RAG.Interfaces;

/// <summary>
/// Interface for loading documents from database and static files for RAG indexing
/// </summary>
public interface IRagDocumentLoader
{
    /// <summary>
    /// Load all admission-related documents from database and static files
    /// </summary>
    /// <returns>Collection of RagDocument objects ready for embedding</returns>
    Task<IEnumerable<RagDocument>> LoadDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Load documents from database (Programs, Majors, AdmissionTypes)
    /// </summary>
    Task<IEnumerable<RagDocument>> LoadFromDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Load documents from static files (admission rules, FAQ, policies)
    /// </summary>
    Task<IEnumerable<RagDocument>> LoadFromFilesAsync(CancellationToken cancellationToken = default);
}

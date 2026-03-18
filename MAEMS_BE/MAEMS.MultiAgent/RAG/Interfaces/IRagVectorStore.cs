using MAEMS.MultiAgent.RAG.Models;

namespace MAEMS.MultiAgent.RAG.Interfaces;

/// <summary>
/// Interface for vector store operations (storing and retrieving embeddings)
/// </summary>
public interface IRagVectorStore
{
    /// <summary>
    /// Index documents with their embeddings in the vector store
    /// </summary>
    /// <param name="documents">Documents with embeddings to index</param>
    Task IndexDocumentsAsync(IEnumerable<RagDocumentWithEmbedding> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve similar documents based on embedding vector
    /// </summary>
    /// <param name="embedding">Query embedding vector</param>
    /// <param name="topK">Number of similar documents to retrieve (default 5)</param>
    /// <returns>List of similar documents with similarity scores</returns>
    Task<IEnumerable<RagDocumentSimilarity>> SearchAsync(float[] embedding, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear all documents from the vector store
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total number of documents in vector store
    /// </summary>
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a document by ID
    /// </summary>
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Document with embedding vector
/// </summary>
public record RagDocumentWithEmbedding(RagDocument Document, float[] Embedding);

/// <summary>
/// Search result with similarity score
/// </summary>
public record RagDocumentSimilarity(RagDocument Document, float Score);

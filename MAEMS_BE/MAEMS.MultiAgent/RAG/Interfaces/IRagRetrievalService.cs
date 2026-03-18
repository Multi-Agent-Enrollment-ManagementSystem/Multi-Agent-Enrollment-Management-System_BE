using MAEMS.MultiAgent.RAG.Models;

namespace MAEMS.MultiAgent.RAG.Interfaces;

/// <summary>
/// Interface for retrieving relevant documents from the vector store
/// </summary>
public interface IRagRetrievalService
{
    /// <summary>
    /// Retrieve relevant documents for a query
    /// </summary>
    /// <param name="query">User query text</param>
    /// <param name="topK">Number of documents to retrieve</param>
    /// <returns>List of relevant documents ranked by similarity</returns>
    Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve and format documents as context for LLM
    /// </summary>
    /// <param name="query">User query</param>
    /// <param name="topK">Number of documents</param>
    /// <returns>Formatted context string for inclusion in prompts</returns>
    Task<string> RetrieveAsContextAsync(string query, int topK = 5, CancellationToken cancellationToken = default);

    /// <summary>
    /// Index new documents into the vector store
    /// </summary>
    Task IndexDocumentsAsync(IEnumerable<RagDocument> documents, CancellationToken cancellationToken = default);
}

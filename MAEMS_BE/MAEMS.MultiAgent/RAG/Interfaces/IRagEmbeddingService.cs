using MAEMS.MultiAgent.RAG.Models;

namespace MAEMS.MultiAgent.RAG.Interfaces;

/// <summary>
/// Interface for generating embeddings for RAG documents
/// </summary>
public interface IRagEmbeddingService
{
    /// <summary>
    /// Generate embedding vector for a text string
    /// </summary>
    /// <param name="text">Text to embed</param>
    /// <returns>Embedding vector (float array)</returns>
    Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts
    /// </summary>
    /// <param name="texts">Collection of texts to embed</param>
    /// <returns>Array of embedding vectors</returns>
    Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get embedding model name
    /// </summary>
    string GetModelName();
}

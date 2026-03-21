namespace MAEMS.MultiAgent.RAG.Interfaces;

/// <summary>
/// Interface for embedding cache service
/// </summary>
public interface IEmbeddingCacheService
{
    /// <summary>
    /// Get cache key for text
    /// </summary>
    string GetCacheKey(string text);

    /// <summary>
    /// Try to get cached embedding
    /// </summary>
    bool TryGetCachedEmbedding(string text, out float[]? embedding);

    /// <summary>
    /// Cache embedding
    /// </summary>
    void CacheEmbedding(string text, float[] embedding);

    /// <summary>
    /// Get cache stats (count, size in MB)
    /// </summary>
    (int CachedCount, int CacheSize) GetCacheStats();

    /// <summary>
    /// Save cache to file
    /// </summary>
    Task SaveCacheAsync();

    /// <summary>
    /// Clear cache
    /// </summary>
    void ClearCache();
}

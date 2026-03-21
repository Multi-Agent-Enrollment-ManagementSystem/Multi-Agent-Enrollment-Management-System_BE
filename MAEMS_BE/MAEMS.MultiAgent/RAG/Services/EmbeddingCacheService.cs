using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using MAEMS.MultiAgent.RAG.Interfaces;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Caches embeddings to reduce API calls to Gemini
/// Uses SHA256 hash of text as key for consistency
/// </summary>
public class EmbeddingCacheService : IEmbeddingCacheService
{
    private readonly ConcurrentDictionary<string, float[]> _cache;
    private readonly ILogger<EmbeddingCacheService> _logger;
    private readonly string _cacheFilePath;

    public EmbeddingCacheService(ILogger<EmbeddingCacheService> logger)
    {
        _logger = logger;
        _cache = new ConcurrentDictionary<string, float[]>();
        _cacheFilePath = Path.Combine(Path.GetTempPath(), "embedding_cache.json");
        LoadCacheFromFile();
    }

    /// <summary>
    /// Get hash key for text
    /// </summary>
    public string GetCacheKey(string text)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Try to get cached embedding
    /// </summary>
    public bool TryGetCachedEmbedding(string text, out float[]? embedding)
    {
        var key = GetCacheKey(text);
        if (_cache.TryGetValue(key, out var cachedEmbedding))
        {
            _logger.LogDebug($"Cache hit for text: {text.Substring(0, Math.Min(30, text.Length))}...");
            embedding = cachedEmbedding;
            return true;
        }

        embedding = null;
        return false;
    }

    /// <summary>
    /// Store embedding in cache
    /// </summary>
    public void CacheEmbedding(string text, float[] embedding)
    {
        var key = GetCacheKey(text);
        _cache.AddOrUpdate(key, embedding, (k, v) => embedding);
        _logger.LogDebug($"Cached embedding for text: {text.Substring(0, Math.Min(30, text.Length))}...");
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int CachedCount, int CacheSize) GetCacheStats()
    {
        var count = _cache.Count;
        var size = _cache.Sum(kvp => kvp.Value.Length * sizeof(float)) / (1024 * 1024); // MB
        return (count, size);
    }

    /// <summary>
    /// Save cache to file for persistence
    /// </summary>
    public async Task SaveCacheAsync()
    {
        try
        {
            var cacheData = _cache.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(f => f.ToString()).ToArray()
            );

            var json = System.Text.Json.JsonSerializer.Serialize(cacheData, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            await File.WriteAllTextAsync(_cacheFilePath, json);
            var (count, size) = GetCacheStats();
            _logger.LogInformation($"Saved {count} cached embeddings to {_cacheFilePath} ({size}MB)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save embedding cache");
        }
    }

    /// <summary>
    /// Load cache from file
    /// </summary>
    private void LoadCacheFromFile()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                _logger.LogInformation($"No existing cache file at {_cacheFilePath}");
                return;
            }

            var json = File.ReadAllText(_cacheFilePath);
            var cacheData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string[]>>(json);

            if (cacheData != null)
            {
                foreach (var kvp in cacheData)
                {
                    var embedding = kvp.Value.Select(f => float.Parse(f)).ToArray();
                    _cache.TryAdd(kvp.Key, embedding);
                }

                var (count, size) = GetCacheStats();
                _logger.LogInformation($"Loaded {count} cached embeddings from {_cacheFilePath} ({size}MB)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load embedding cache");
        }
    }

    /// <summary>
    /// Clear all cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Cleared embedding cache");
    }
}

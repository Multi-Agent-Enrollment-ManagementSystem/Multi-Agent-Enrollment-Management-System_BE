namespace MAEMS.MultiAgent.RAG.Configuration;

/// <summary>
/// Configuration settings for RAG system
/// </summary>
public class RagSettings
{
    public const string SectionName = "RagSettings";

    /// <summary>
    /// Chunk size in tokens for document splitting (default: 512)
    /// </summary>
    public int ChunkSizeTokens { get; set; } = 512;

    /// <summary>
    /// Overlap between chunks in tokens (default: 50)
    /// </summary>
    public int ChunkOverlapTokens { get; set; } = 50;

    /// <summary>
    /// Qdrant vector store settings
    /// </summary>
    public QdrantSettings Qdrant { get; set; } = new();

    /// <summary>
    /// Embedding settings
    /// </summary>
    public EmbeddingSettings Embedding { get; set; } = new();

    /// <summary>
    /// Number of documents to retrieve by default (default: 5)
    /// </summary>
    public int DefaultTopK { get; set; } = 5;

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0, default: 0.3)
    /// </summary>
    public float MinSimilarityScore { get; set; } = 0.3f;

    /// <summary>
    /// Enable automatic indexing on startup
    /// </summary>
    public bool EnableAutoIndexing { get; set; } = true;

    /// <summary>
    /// Indexing interval in minutes (default: 60 = 1 hour)
    /// </summary>
    public int IndexingIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Qdrant vector database settings
/// </summary>
public class QdrantSettings
{
    /// <summary>
    /// Qdrant server URL (default: http://localhost:6333)
    /// </summary>
    public string Url { get; set; } = "http://localhost:6333";

    /// <summary>
    /// Collection name for admission documents (default: "admission_documents")
    /// </summary>
    public string CollectionName { get; set; } = "admission_documents";

    /// <summary>
    /// Vector dimension for embeddings (must match embedding model output)
    /// Gemini embeddings produce 768-dimensional vectors
    /// </summary>
    public int VectorDimension { get; set; } = 768;

    /// <summary>
    /// Vector similarity metric (cosine, euclidean, dot)
    /// </summary>
    public string VectorMetric { get; set; } = "cosine";
}

/// <summary>
/// Embedding service settings
/// </summary>
public class EmbeddingSettings
{
    /// <summary>
    /// Embedding model name (default: "gemini-embedding-001" for Gemini v1beta)
    /// </summary>
    public string ModelName { get; set; } = "gemini-embedding-001";

    /// <summary>
    /// Text type for Gemini embeddings (default: "SEMANTIC_SEARCH")
    /// </summary>
    public string TaskType { get; set; } = "SEMANTIC_SEARCH";

    /// <summary>
    /// Maximum texts to embed in batch (default: 5, reduced from 50 to avoid rate limiting)
    /// </summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>
    /// Delay in milliseconds between individual embedding requests within a batch (default: 500ms)
    /// Helps avoid rate limiting from Gemini API
    /// </summary>
    public int DelayBetweenRequestsMs { get; set; } = 500;

    /// <summary>
    /// Delay in milliseconds between batches (default: 1000ms = 1 second)
    /// Helps avoid rate limiting from Gemini API when indexing large datasets
    /// </summary>
    public int DelayBetweenBatchesMs { get; set; } = 1000;
}


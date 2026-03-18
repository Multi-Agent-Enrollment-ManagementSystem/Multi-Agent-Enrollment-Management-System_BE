using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MAEMS.MultiAgent.RAG.Configuration;
using MAEMS.MultiAgent.RAG.Interfaces;
using MAEMS.MultiAgent.RAG.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Vector store service using Qdrant for document embedding storage and retrieval
/// </summary>
public class RagVectorStore : IRagVectorStore
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagVectorStore> _logger;
    private readonly QdrantSettings _qdrantSettings;
    private readonly string _collectionName;

    public RagVectorStore(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RagVectorStore> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var ragSettings = new RagSettings();
        _configuration.GetSection(RagSettings.SectionName).Bind(ragSettings);

        _qdrantSettings = ragSettings.Qdrant;
        _collectionName = _qdrantSettings.CollectionName;

        _httpClient.BaseAddress = new Uri(_qdrantSettings.Url);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task IndexDocumentsAsync(IEnumerable<RagDocumentWithEmbedding> documents, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Indexing documents into Qdrant collection '{_collectionName}'");

        try
        {
            // Ensure collection exists
            await EnsureCollectionExistsAsync(cancellationToken);

            var documentList = documents.ToList();

            if (documentList.Count == 0)
            {
                _logger.LogWarning("No documents to index");
                return;
            }

            // Convert to Qdrant point format
            var points = documentList.Select((doc, index) => new QdrantPoint
            {
                Id = (ulong)(index + 1), // Qdrant requires numeric IDs
                Vector = doc.Embedding,
                Payload = new QdrantPayload
                {
                    DocumentId = doc.Document.Id,
                    Content = doc.Document.Content,
                    Source = doc.Document.Source,
                    Metadata = doc.Document.Metadata,
                    CreatedAt = doc.Document.CreatedAt.ToString("O")
                }
            }).ToList();

            // Upsert points to collection
            var upsertRequest = new
            {
                points = points
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"/collections/{_collectionName}/points",
                upsertRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"Failed to index documents: {response.StatusCode} - {errorContent}");
                throw new InvalidOperationException($"Qdrant indexing failed: {response.StatusCode}");
            }

            _logger.LogInformation($"Successfully indexed {points.Count} documents");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while indexing documents");
            throw new InvalidOperationException("Failed to connect to Qdrant vector store", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing documents");
            throw;
        }
    }

    public async Task<IEnumerable<RagDocumentSimilarity>> SearchAsync(float[] embedding, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (embedding == null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding cannot be empty", nameof(embedding));
        }

        _logger.LogDebug($"Searching for {topK} similar documents in Qdrant");

        try
        {
            var searchRequest = new
            {
                vector = embedding,
                limit = topK,
                with_payload = true,
                with_vectors = false
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/collections/{_collectionName}/points/search",
                searchRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"Search failed: {response.StatusCode} - {errorContent}");
                throw new InvalidOperationException($"Qdrant search failed: {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResults = JsonSerializer.Deserialize<QdrantSearchResponse>(responseString) 
                ?? throw new InvalidOperationException("Failed to parse search response");
            using var jsonDoc = JsonDocument.Parse(responseString);
            if (!jsonDoc.RootElement.TryGetProperty("result", out var resultElement))
            {
                searchResults.Result = new List<QdrantSearchResult>();
            }

            var results = new List<RagDocumentSimilarity>();

            foreach (var result in searchResults.Result)
            {
                var document = new RagDocument
                {
                    Id = result.Payload.DocumentId,
                    Content = result.Payload.Content,
                    Source = result.Payload.Source,
                    Metadata = result.Payload.Metadata,
                    CreatedAt = DateTime.Parse(result.Payload.CreatedAt)
                };

                results.Add(new RagDocumentSimilarity(document, result.Score));
            }

            _logger.LogDebug($"Found {results.Count} similar documents");
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while searching");
            throw new InvalidOperationException("Failed to search vector store", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            throw;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"Clearing collection '{_collectionName}'");

        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/collections/{_collectionName}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Collection cleared successfully");
                // Recreate the collection
                await EnsureCollectionExistsAsync(cancellationToken);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"Failed to clear collection: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing collection");
            throw;
        }
    }

    public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/collections/{_collectionName}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to get collection info: {response.StatusCode}");
                return 0;
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var collectionInfo = JsonSerializer.Deserialize<QdrantCollectionInfo>(responseString)
                ?? throw new InvalidOperationException("Failed to parse collection info");
            return (int)collectionInfo.Result.PointsCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count");
            return 0;
        }
    }

    public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug($"Deleting document {documentId} from vector store");

        try
        {
            // Qdrant deletes by payload filter
            var deleteRequest = new
            {
                filter = new
                {
                    must = new[]
                    {
                        new
                        {
                            key = "document_id",
                            match = new { value = documentId }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/collections/{_collectionName}/points/delete",
                deleteRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to delete document: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document");
        }
    }

    private async Task EnsureCollectionExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/collections/{_collectionName}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug($"Collection '{_collectionName}' already exists");
                return;
            }

            _logger.LogInformation($"Creating collection '{_collectionName}' with vector dimension {_qdrantSettings.VectorDimension}");

            var createRequest = new
            {
                vectors = new
                {
                    size = _qdrantSettings.VectorDimension,
                    distance = _qdrantSettings.VectorMetric
                }
            };

            var createResponse = await _httpClient.PutAsJsonAsync(
                $"/collections/{_collectionName}",
                createRequest,
                cancellationToken);

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError($"Failed to create collection: {createResponse.StatusCode} - {errorContent}");
                throw new InvalidOperationException($"Failed to create Qdrant collection: {createResponse.StatusCode}");
            }

            _logger.LogInformation($"Collection '{_collectionName}' created successfully");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while ensuring collection exists");
            throw new InvalidOperationException("Failed to connect to Qdrant", ex);
        }
    }
}

// Qdrant API Models
internal record QdrantPoint
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("vector")]
    public required float[] Vector { get; set; }

    [JsonPropertyName("payload")]
    public required QdrantPayload Payload { get; set; }
}

internal record QdrantPayload
{
    [JsonPropertyName("document_id")]
    public required string DocumentId { get; set; }

    [JsonPropertyName("content")]
    public required string Content { get; set; }

    [JsonPropertyName("source")]
    public required string Source { get; set; }

    [JsonPropertyName("metadata")]
    public required Dictionary<string, string> Metadata { get; set; }

    [JsonPropertyName("created_at")]
    public required string CreatedAt { get; set; }
}

internal record QdrantSearchResult
{
    [JsonPropertyName("id")]
    public ulong Id { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }

    [JsonPropertyName("payload")]
    public required QdrantPayload Payload { get; set; }
}

internal record QdrantSearchResponse
{
    [JsonPropertyName("result")]
    public required List<QdrantSearchResult> Result { get; set; }
}

internal record QdrantCollectionInfo
{
    [JsonPropertyName("result")]
    public required QdrantCollectionResult Result { get; set; }
}

internal record QdrantCollectionResult
{
    [JsonPropertyName("points_count")]
    public long PointsCount { get; set; }
}

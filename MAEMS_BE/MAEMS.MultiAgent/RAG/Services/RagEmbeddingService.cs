using System.Text;
using System.Text.Json;
using MAEMS.MultiAgent.RAG.Configuration;
using MAEMS.MultiAgent.RAG.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Service for generating embeddings using Google Gemini Embedding API
/// </summary>
public class RagEmbeddingService : IRagEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagEmbeddingService> _logger;
    private readonly RagSettings _ragSettings;
    private readonly string _apiKey;
    private readonly string _apiUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public RagEmbeddingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<RagEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _ragSettings = new RagSettings();
        _configuration.GetSection(RagSettings.SectionName).Bind(_ragSettings);

        _apiKey = _configuration["GeminiService:ApiKey"] 
            ?? throw new InvalidOperationException("GeminiService:ApiKey is not configured");

        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        var embeddings = await EmbedTextsAsync(new[] { text }, cancellationToken);
        return embeddings[0];
    }

    public async Task<float[][]> EmbedTextsAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        if (textList.Count == 0)
        {
            throw new ArgumentException("At least one non-empty text is required", nameof(texts));
        }

        _logger.LogInformation($"Embedding {textList.Count} texts using Gemini API");

        var embeddings = new List<float[]>();

        try
        {
            // Process texts in batches (Gemini limit is 100)
            var batchSize = _ragSettings.Embedding.BatchSize;

            for (int i = 0; i < textList.Count; i += batchSize)
            {
                var batch = textList.Skip(i).Take(batchSize).ToList();
                var batchEmbeddings = await EmbedBatchAsync(batch, cancellationToken);
                embeddings.AddRange(batchEmbeddings);
            }

            _logger.LogInformation($"Successfully embedded {embeddings.Count} texts");
            return embeddings.ToArray();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while embedding texts");
            throw new InvalidOperationException("Failed to embed texts: HTTP request failed", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error in embedding response");
            throw new InvalidOperationException("Failed to parse embedding response", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while embedding texts");
            throw;
        }
    }

    public string GetModelName() => _ragSettings.Embedding.ModelName;

    private async Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken)
    {
        var embeddings = new List<float[]>();
        var textList = texts.ToList();

        // Use gemini-embedding-001 (available in v1beta, supports embedContent)
        var modelName = "gemini-embedding-001";

        // Process each text individually
        foreach (var text in textList)
        {
            try
            {
                var url = $"{_apiUrl}/{modelName}:embedContent?key={_apiKey}";

                var requestPayload = new
                {
                    model = $"models/{modelName}",
                    content = new
                    {
                        parts = new[] { new { text = text } }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug($"Embedding text with {modelName}: {text.Substring(0, Math.Min(50, text.Length))}...");

                var response = await _httpClient.PostAsync(url, httpContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError($"Embedding API returned {response.StatusCode}: {errorContent}");
                    throw new InvalidOperationException($"Embedding API error: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var responseJson = JsonDocument.Parse(responseContent);
                var root = responseJson.RootElement;

                // Parse the response: { "embedding": { "values": [...] } }
                if (root.TryGetProperty("embedding", out var embeddingObj))
                {
                    if (embeddingObj.TryGetProperty("values", out var valuesArray))
                    {
                        var values = new List<float>();
                        foreach (var value in valuesArray.EnumerateArray())
                        {
                            if (value.TryGetSingle(out var floatValue))
                            {
                                values.Add(floatValue);
                            }
                        }

                        if (values.Count > 0)
                        {
                            embeddings.Add(values.ToArray());
                            _logger.LogDebug($"Generated embedding with {values.Count} dimensions");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error embedding individual text");
                throw;
            }
        }

        return embeddings;
    }
}

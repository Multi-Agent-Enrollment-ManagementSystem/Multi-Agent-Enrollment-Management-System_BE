using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.Infrastructure.Services;

public interface IGeminiService
{
    Task<string> GetResponseAsync(
        string userMessage,
        List<(string role, string content)> conversationHistory,
        string systemPrompt,
        CancellationToken cancellationToken = default);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly string _apiUrl;

    public GeminiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _apiKey = configuration["GeminiService:ApiKey"]
            ?? throw new InvalidOperationException("GeminiService:ApiKey not configured");
        _modelName = configuration["GeminiService:ModelName"]
            ?? throw new InvalidOperationException("GeminiService:ModelName not configured");
        _apiUrl = configuration["GeminiService:ApiUrl"]
            ?? throw new InvalidOperationException("GeminiService:ApiUrl not configured");

        // Set timeout
        var timeoutSeconds = configuration.GetValue<int>("GeminiService:TimeoutSeconds", 60);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public async Task<string> GetResponseAsync(
        string userMessage,
        List<(string role, string content)> conversationHistory,
        string systemPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build messages array
            var messages = new List<object>();

            // Add system instruction as first user message
            messages.Add(new
            {
                role = "user",
                parts = new object[] 
                { 
                    new { text = systemPrompt } 
                }
            });

            // Model acknowledges
            messages.Add(new
            {
                role = "model",
                parts = new object[] 
                { 
                    new { text = "Hiểu rồi. Tôi sẽ tuân theo hướng dẫn trên." } 
                }
            });

            // Add conversation history
            foreach (var (role, content) in conversationHistory)
            {
                messages.Add(new
                {
                    role = role == "user" ? "user" : "model",
                    parts = new object[] { new { text = content } }
                });
            }

            // Add current user message
            messages.Add(new
            {
                role = "user",
                parts = new object[] { new { text = userMessage } }
            });

            // Build request with proper format for Gemini
            var requestBody = new
            {
                contents = messages,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048,
                    topP = 0.95,
                    topK = 64
                }
            };

            var url = $"{_apiUrl}/{_modelName}:generateContent?key={_apiKey}";

            _logger.LogInformation("📤 Calling Gemini API - Model: {ModelName}", _modelName);
            _logger.LogDebug("📍 URL: {Url}", url.Replace(_apiKey, "***"));

            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Gemini API error {StatusCode}:\n{ErrorContent}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API error {response.StatusCode}: {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("📥 Raw response: {Response}", jsonResponse);

            var jsonDocument = JsonDocument.Parse(jsonResponse);
            var root = jsonDocument.RootElement;

            // Extract text from response
            var responseText = root
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            _logger.LogInformation("✅ Gemini API responded successfully");
            return responseText ?? "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ HTTP error calling Gemini - Check API key, URL, network");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "❌ JSON parsing error");
            throw;
        }
        catch (IndexOutOfRangeException ex)
        {
            _logger.LogError(ex, "❌ Invalid response format from Gemini");
            throw new InvalidOperationException("Invalid response from Gemini API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error: {Type}: {Message}", ex.GetType().Name, ex.Message);
            throw;
        }
    }
}

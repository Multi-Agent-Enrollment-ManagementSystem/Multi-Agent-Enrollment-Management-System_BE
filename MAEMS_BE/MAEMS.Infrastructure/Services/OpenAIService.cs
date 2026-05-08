using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MAEMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.Infrastructure.Services
{
    /// <summary>
    /// OpenAI service implementation - CHAT ONLY
    /// Embeddings still use Gemini to avoid re-indexing 254 documents
    /// </summary>
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _apiKey;
        private readonly string _chatModel;
        private readonly int _timeoutSeconds;

        public OpenAIService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Read configuration
            _apiKey = configuration["OpenAIService:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI API key not configured in appsettings.json");
            _chatModel = configuration["OpenAIService:ChatModel"] ?? "gpt-4o-mini";
            _timeoutSeconds = int.Parse(configuration["OpenAIService:TimeoutSeconds"] ?? "90");

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        }

        public async Task<string> GetChatCompletionAsync(
            string systemPrompt,
            string userMessage,
            List<(string role, string content)> conversationHistory = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build messages array
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Add conversation history if provided
                if (conversationHistory != null && conversationHistory.Any())
                {
                    foreach (var (role, content) in conversationHistory)
                    {
                        messages.Add(new { role, content });
                    }
                }

                // Add current user message
                messages.Add(new { role = "user", content = userMessage });

                // Build request payload (low temperature for deterministic responses)
                var requestBody = new
                {
                    model = _chatModel,
                    messages = messages,
                    temperature = 0.1,
                    max_tokens = maxTokens ?? 2000
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling OpenAI chat API with model {Model}", _chatModel);

                // Call OpenAI API
                var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JsonDocument.Parse(responseContent);

                // Extract assistant's reply
                var assistantReply = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation("OpenAI chat API call successful");

                return assistantReply ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling OpenAI chat API");
                throw new InvalidOperationException("Failed to get chat completion from OpenAI", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI chat API call timed out");
                throw new TimeoutException("OpenAI chat API call timed out", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI chat API");
                throw;
            }
        }

        /// <summary>
        /// Call OpenAI Vision API with images (for document analysis)
        /// </summary>
        public async Task<string> GetVisionCompletionAsync(
            string systemPrompt,
            string userMessage,
            List<string> base64Images,
            int? maxTokens = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Build messages array with vision content
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                // Build user message with images
                var contentParts = new List<object>
                {
                    new { type = "text", text = userMessage }
                };

                foreach (var base64Image in base64Images)
                {
                    contentParts.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:image/jpeg;base64,{base64Image}"
                        }
                    });
                }

                messages.Add(new { role = "user", content = contentParts });

                // Build request payload (use configured chat model, low temp for extraction, reduced tokens for JSON)
                var requestBody = new
                {
                    model = _chatModel,
                    messages = messages,
                    temperature = 0.1,
                    max_tokens = maxTokens ?? 1500
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("Calling OpenAI Vision API with {ImageCount} images", base64Images.Count);

                // Call OpenAI API
                var response = await _httpClient.PostAsync("chat/completions", httpContent, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonResponse = JsonDocument.Parse(responseContent);

                // Extract assistant's reply
                var assistantReply = jsonResponse.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                _logger.LogInformation("OpenAI Vision API call successful");

                return assistantReply ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling OpenAI Vision API");
                throw new InvalidOperationException("Failed to get vision completion from OpenAI", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI Vision API call timed out");
                throw new TimeoutException("OpenAI Vision API call timed out", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling OpenAI Vision API");
                throw;
            }
        }
    }
}

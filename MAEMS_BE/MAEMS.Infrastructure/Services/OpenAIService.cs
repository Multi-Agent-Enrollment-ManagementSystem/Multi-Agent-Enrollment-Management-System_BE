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
            _timeoutSeconds = int.Parse(configuration["OpenAIService:TimeoutSeconds"] ?? "30");

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        }

        public async Task<string> GetChatCompletionAsync(
            string systemPrompt,
            string userMessage,
            List<(string role, string content)> conversationHistory = null,
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

                // Build request payload
                var requestBody = new
                {
                    model = _chatModel,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 2000
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
    }
}

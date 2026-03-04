using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAEMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Application Intake Agent — gửi file tài liệu kèm prompt cho Ollama LLM,
/// nhận lại JSON kiểm tra chất lượng và nhận dạng loại tài liệu.
/// <list type="bullet">
///   <item>Ảnh (.jpg/.jpeg/.png) → encode base64 gửi thẳng vào <c>images[]</c>.</item>
///   <item>PDF → render từng trang thành PNG qua <see cref="DocumentIntakeAgentPdfConverter"/>, rồi gửi vào <c>images[]</c>.</item>
/// </list>
/// </summary>
public sealed class DocumentIntakeAgent : IDocumentIntakeAgent
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentIntakeAgent> _logger;
    private readonly DocumentIntakeAgentPdfConverter _pdfConverter;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _modelName;

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    private static readonly HashSet<string> PdfExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    private static readonly JsonSerializerOptions RequestSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions ResponseDeserializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public DocumentIntakeAgent(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<DocumentIntakeAgent> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pdfConverter = new DocumentIntakeAgentPdfConverter(logger);

        _apiUrl = configuration["Ollama:ApiUrl"]
            ?? throw new InvalidOperationException("Ollama:ApiUrl is not configured");
        _apiKey = configuration["Ollama:ApiKey"]
            ?? throw new InvalidOperationException("Ollama:ApiKey is not configured");
        _modelName = configuration["Ollama:ModelName"]
            ?? throw new InvalidOperationException("Ollama:ModelName is not configured");
    }

    /// <inheritdoc />
    public async Task<DocumentQualityCheckResult> CheckDocumentQualityAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var imageBase64List = await PrepareImagesAsync(file, cancellationToken);

            var responseBody = await CallOllamaAsync(imageBase64List, file.FileName, cancellationToken);

            return ParseLlmResponse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentIntakeAgent: Error processing '{FileName}'", file.FileName);
            throw;
        }
    }

    // ── Step 1: Chuẩn bị danh sách ảnh base64 ────────────────────────────────

    private async Task<List<string>> PrepareImagesAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        byte[] fileBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, cancellationToken);
            fileBytes = ms.ToArray();
        }

        var ext = Path.GetExtension(file.FileName);

        if (ImageExtensions.Contains(ext))
        {
            _logger.LogInformation(
                "DocumentIntakeAgent: [IMAGE] '{FileName}' ({Size} bytes) → model '{Model}'",
                file.FileName, fileBytes.Length, _modelName);

            return [Convert.ToBase64String(fileBytes)];
        }

        if (PdfExtensions.Contains(ext))
        {
            var pages = _pdfConverter.Convert(fileBytes, file.FileName);

            _logger.LogInformation(
                "DocumentIntakeAgent: [PDF] '{FileName}' ({Size} bytes) — {Pages} page(s) → model '{Model}'",
                file.FileName, fileBytes.Length, pages.Count, _modelName);

            return pages;
        }

        throw new NotSupportedException(
            $"File type '{ext}' is not supported. Allowed: " +
            string.Join(", ", ImageExtensions.Concat(PdfExtensions)));
    }

    // ── Step 2: Gọi Ollama API ────────────────────────────────────────────────

    private async Task<string> CallOllamaAsync(
        List<string> imageBase64List,
        string fileName,
        CancellationToken cancellationToken)
    {
        var requestBody = BuildRequest(imageBase64List);
        var json = JsonSerializer.Serialize(requestBody, RequestSerializerOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        // Đọc body TRƯỚC khi kiểm tra status để log được lỗi chi tiết từ Ollama
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "DocumentIntakeAgent: Ollama returned HTTP {StatusCode} for '{FileName}'. Body: {Body}",
                (int)response.StatusCode, fileName, responseBody);

            throw new HttpRequestException(
                $"Ollama API error {(int)response.StatusCode}: {responseBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        _logger.LogDebug("DocumentIntakeAgent: Raw LLM response — {Response}", responseBody);
        return responseBody;
    }

    // ── Step 3: Parse response ────────────────────────────────────────────────

    private DocumentQualityCheckResult ParseLlmResponse(string responseBody)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, ResponseDeserializerOptions)
                ?? throw new InvalidOperationException("Ollama response could not be deserialized.");

            // Ollama native: message.content
            // OpenAI-compatible fallback: choices[0].message.content
            var content = envelope.Message?.Content
                ?? envelope.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("LLM returned an empty content field.");

            content = StripMarkdownFences(content);

            var llmResult = JsonSerializer.Deserialize<LlmQualityCheckResponse>(content, ResponseDeserializerOptions)
                ?? throw new InvalidOperationException("LLM inner JSON could not be deserialized.");

            return MapToResult(llmResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocumentIntakeAgent: Failed to parse LLM response. Body: {Body}", responseBody);
            throw new InvalidOperationException($"Failed to parse LLM quality-check response: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Build Ollama /api/chat request body.
    /// images[] nhận raw base64 — KHÔNG có "data:...;base64," prefix.
    /// </summary>
    private OllamaChatRequest BuildRequest(List<string> imageBase64List) => new()
    {
        Model = _modelName,
        Stream = false,
        Messages =
        [
            new OllamaMessage
            {
                Role    = "system",
                Content = DocumentIntakeAgentPrompts.QualityCheck
            },
            new OllamaMessage
            {
                Role   = "user",
                Content = "Please analyze the attached document.",
                Images = imageBase64List
            }
        ]
    };

    private static DocumentQualityCheckResult MapToResult(LlmQualityCheckResponse llm) => new()
    {
        DocumentType       = llm.DocumentType ?? "other",
        PassedQualityCheck = llm.PassedQualityCheck,
        Confidence         = llm.Confidence,
        Issues             = llm.Issues ?? [],
        Quality = new DocumentQuality
        {
            IsReadable   = llm.Quality?.IsReadable   ?? false,
            IsUnobscured = llm.Quality?.IsUnobscured ?? false,
            IsUnblurred  = llm.Quality?.IsUnblurred  ?? false,
            IsComplete   = llm.Quality?.IsComplete   ?? false,
            IsUnedited   = llm.Quality?.IsUnedited   ?? false
        }
    };

    private static string StripMarkdownFences(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
                trimmed = trimmed[(firstNewline + 1)..];
            if (trimmed.EndsWith("```"))
                trimmed = trimmed[..^3].TrimEnd();
        }
        return trimmed.Trim();
    }
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Document Verification Agent — tải từng document từ Firebase, gửi kèm thông tin applicant
/// cho Ollama LLM để cross-check, rồi lưu kết quả vào DB.
/// <para>
/// Chạy hoàn toàn độc lập (fire-and-forget) — caller không cần await.
/// Dùng <see cref="IServiceScopeFactory"/> để tạo scope riêng, tránh conflict với
/// DbContext của request gốc đã được dispose.
/// </para>
/// </summary>
public sealed class DocumentVerificationAgent : IDocumentVerificationAgent
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentVerificationAgent> _logger;
    private readonly DocumentIntakeAgentPdfConverter _pdfConverter;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _modelName;

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    private static readonly HashSet<string> PdfExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions DeserializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public DocumentVerificationAgent(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<DocumentVerificationAgent> logger)
    {
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
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
    /// <remarks>
    /// Fire-and-forget: caller calls without await.
    /// Internally uses Task.Run so the background work survives the request pipeline.
    /// </remarks>
    public Task VerifyApplicationDocumentsAsync(int applicationId)
    {
        // Detach from the caller's synchronization context entirely
        _ = Task.Run(() => RunVerificationAsync(applicationId));
        return Task.CompletedTask;
    }

    // ── Core background work ──────────────────────────────────────────────────

    private async Task RunVerificationAsync(int applicationId)
    {
        _logger.LogInformation(
            "DocumentVerificationAgent: Starting verification for ApplicationId={ApplicationId}",
            applicationId);

        // Create a fresh DI scope — the original request scope is already gone
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // ── Load application + applicant ──────────────────────────────
            var application = await unitOfWork.Applications.GetByIdAsync(applicationId);
            if (application == null)
            {
                _logger.LogWarning(
                    "DocumentVerificationAgent: ApplicationId={ApplicationId} not found, aborting.",
                    applicationId);
                return;
            }

            Applicant? applicant = null;
            if (application.ApplicantId.HasValue)
                applicant = await unitOfWork.Applicants.GetByIdAsync(application.ApplicantId.Value);

            if (applicant == null)
            {
                _logger.LogWarning(
                    "DocumentVerificationAgent: Applicant not found for ApplicationId={ApplicationId}, aborting.",
                    applicationId);
                return;
            }

            var applicantJson = BuildApplicantJson(applicant);

            // ── Load all documents for this application ───────────────────
            var documents = (await unitOfWork.Documents.GetByApplicationIdAsync(applicationId)).ToList();

            if (documents.Count == 0)
            {
                _logger.LogInformation(
                    "DocumentVerificationAgent: No documents found for ApplicationId={ApplicationId}.",
                    applicationId);
                return;
            }

            _logger.LogInformation(
                "DocumentVerificationAgent: Verifying {Count} document(s) for ApplicationId={ApplicationId}",
                documents.Count, applicationId);

            // ── Verify each document sequentially ────────────────────────
            foreach (var document in documents)
            {
                await VerifySingleDocumentAsync(document, applicantJson, unitOfWork);
            }

            _logger.LogInformation(
                "DocumentVerificationAgent: Verification complete for ApplicationId={ApplicationId}",
                applicationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DocumentVerificationAgent: Unhandled error during verification for ApplicationId={ApplicationId}",
                applicationId);
        }
    }

    // ── Verify one document ───────────────────────────────────────────────────

    private async Task VerifySingleDocumentAsync(
        Document document,
        string applicantJson,
        IUnitOfWork unitOfWork)
    {
        if (string.IsNullOrWhiteSpace(document.FilePath) || string.IsNullOrWhiteSpace(document.FileName))
        {
            _logger.LogWarning(
                "DocumentVerificationAgent: DocumentId={DocumentId} has no FilePath or FileName, skipping.",
                document.DocumentId);
            return;
        }

        try
        {
            _logger.LogInformation(
                "DocumentVerificationAgent: Verifying DocumentId={DocumentId} '{FileName}' (Type={Type})",
                document.DocumentId, document.FileName, document.DocumentType);

            // Step 1: Download bytes from Firebase public URL
            var fileBytes = await DownloadBytesAsync(document.FilePath, document.FileName);

            // Step 2: Convert bytes → base64 image list (handles images and PDFs)
            var imageBase64List = PrepareImagesFromBytes(fileBytes, document.FileName);

            // Step 3: Call Ollama with applicant profile + document images
            var responseBody = await CallOllamaAsync(imageBase64List, applicantJson, document);

            // Step 4: Parse LLM JSON response
            var verificationResult = ParseLlmResponse(responseBody, document.DocumentId);

            // Step 5: Persist result to DB
            document.VerificationResult  = verificationResult.Result;
            document.VerificationDetails = verificationResult.Details;

            await unitOfWork.Documents.UpdateAsync(document);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "DocumentVerificationAgent: DocumentId={DocumentId} → {Result}{Details}",
                document.DocumentId,
                verificationResult.Result,
                verificationResult.Details != null ? $" | {verificationResult.Details}" : string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DocumentVerificationAgent: Error verifying DocumentId={DocumentId}, marking as 'error'",
                document.DocumentId);

            // Mark as error in DB so it's visible — do NOT crash the loop for remaining documents
            try
            {
                document.VerificationResult  = "error";
                document.VerificationDetails = $"Verification failed: {ex.Message}";
                await unitOfWork.Documents.UpdateAsync(document);
                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx,
                    "DocumentVerificationAgent: Failed to save error state for DocumentId={DocumentId}",
                    document.DocumentId);
            }
        }
    }

    // ── Step 1: Download bytes from Firebase public URL ───────────────────────

    private async Task<byte[]> DownloadBytesAsync(string url, string fileName)
    {
        // Firebase ?alt=media URL is public — no Authorization header needed
        using var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to download '{fileName}' from Firebase Storage. HTTP {(int)response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();

        _logger.LogDebug(
            "DocumentVerificationAgent: Downloaded '{FileName}' ({Size} bytes)",
            fileName, bytes.Length);

        return bytes;
    }

    // ── Step 2: bytes → base64 image list ────────────────────────────────────

    private List<string> PrepareImagesFromBytes(byte[] fileBytes, string fileName)
    {
        var ext = Path.GetExtension(fileName);

        if (ImageExtensions.Contains(ext))
            return [Convert.ToBase64String(fileBytes)];

        if (PdfExtensions.Contains(ext))
            return _pdfConverter.Convert(fileBytes, fileName);

        throw new NotSupportedException(
            $"File type '{ext}' is not supported for verification. Allowed: " +
            string.Join(", ", ImageExtensions.Concat(PdfExtensions)));
    }

    // ── Step 3: Call Ollama ───────────────────────────────────────────────────

    private async Task<string> CallOllamaAsync(
        List<string> imageBase64List,
        string applicantJson,
        Document document)
    {
        var userPrompt =
            $"[APPLICANT_PROFILE]\n{applicantJson}\n\n" +
            $"[DOCUMENT]\nType: {document.DocumentType ?? "unknown"}\nFilename: {document.FileName}\n\n" +
            "Please verify that the information on this document matches the applicant profile.";

        var requestBody = new OllamaChatRequest
        {
            Model  = _modelName,
            Stream = false,
            Messages =
            [
                new OllamaMessage
                {
                    Role    = "system",
                    Content = DocumentVerificationAgentPrompts.Verification
                },
                new OllamaMessage
                {
                    Role    = "user",
                    Content = userPrompt,
                    Images  = imageBase64List
                }
            ]
        };

        var json = JsonSerializer.Serialize(requestBody, SerializerOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ollama API error {(int)response.StatusCode}: {responseBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        _logger.LogDebug(
            "DocumentVerificationAgent: Raw LLM response for DocumentId={DocumentId} — {Response}",
            document.DocumentId, responseBody);

        return responseBody;
    }

    // ── Step 4: Parse LLM response ────────────────────────────────────────────

    private DocumentVerificationResult ParseLlmResponse(string responseBody, int documentId)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<OllamaChatResponse>(responseBody, DeserializerOptions)
                ?? throw new InvalidOperationException("Ollama response could not be deserialized.");

            var content = envelope.Message?.Content
                ?? envelope.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("LLM returned an empty content field.");

            content = StripMarkdownFences(content);

            var llmResult = JsonSerializer.Deserialize<LlmVerificationResponse>(content, DeserializerOptions)
                ?? throw new InvalidOperationException("LLM inner JSON could not be deserialized.");

            // Normalise — defensive against unexpected LLM output
            var result = string.Equals(llmResult.Result, "verified", StringComparison.OrdinalIgnoreCase)
                ? "verified"
                : "rejected";

            return new DocumentVerificationResult
            {
                Result  = result,
                Details = result == "verified" ? null : llmResult.Details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DocumentVerificationAgent: Failed to parse LLM response for DocumentId={DocumentId}. Body: {Body}",
                documentId, responseBody);

            throw new InvalidOperationException(
                $"Failed to parse LLM verification response: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Serialize applicant fields thành JSON string để nhúng vào user prompt.
    /// Chỉ include các field có giá trị để giữ prompt gọn.
    /// </summary>
    private static string BuildApplicantJson(Applicant applicant)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["full_name"]            = applicant.FullName,
            ["date_of_birth"]        = applicant.DateOfBirth?.ToString("yyyy-MM-dd"),
            ["gender"]               = applicant.Gender,
            ["id_issue_number"]      = applicant.IdIssueNumber,
            ["id_issue_date"]        = applicant.IdIssueDate?.ToString("yyyy-MM-dd"),
            ["id_issue_place"]       = applicant.IdIssuePlace,
            ["high_school_name"]     = applicant.HighSchoolName,
            ["high_school_district"] = applicant.HighSchoolDistrict,
            ["high_school_province"] = applicant.HighSchoolProvince,
            ["graduation_year"]      = applicant.GraduationYear,
            ["contact_name"]         = applicant.ContactName,
            ["contact_email"]        = applicant.ContactEmail,
            ["contact_phone"]        = applicant.ContactPhone
        };

        // Strip null/empty values so the prompt stays concise
        var filtered = data
            .Where(kv => kv.Value is not null && kv.Value.ToString() != string.Empty)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return JsonSerializer.Serialize(filtered, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

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

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
/// Sau khi tất cả documents được verify:
/// <list type="bullet">
///   <item>Chuyển <c>Application.Status</c> → <c>"under_review"</c>.</item>
///   <item>Gọi <see cref="IEligibilityEvaluationAgent"/> để đánh giá điều kiện hồ sơ.</item>
/// </list>
/// </para>
/// </summary>
public sealed class DocumentVerificationAgent : IDocumentVerificationAgent
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEligibilityEvaluationAgent _eligibilityAgent;
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
        IEligibilityEvaluationAgent eligibilityAgent,
        IConfiguration configuration,
        ILogger<DocumentVerificationAgent> logger)
    {
        _httpClient = httpClient;
        _scopeFactory = scopeFactory;
        _eligibilityAgent = eligibilityAgent;
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
    public Task VerifyApplicationDocumentsAsync(int applicationId)
    {
        _ = Task.Run(() => RunVerificationAsync(applicationId));
        return Task.CompletedTask;
    }

    // ── Core background work ──────────────────────────────────────────────────

    private async Task RunVerificationAsync(int applicationId)
    {
        _logger.LogInformation(
            "DocumentVerificationAgent: Starting verification for ApplicationId={ApplicationId}",
            applicationId);

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

            if (!application.ApplicantId.HasValue)
            {
                _logger.LogWarning(
                    "DocumentVerificationAgent: ApplicationId={ApplicationId} has no ApplicantId, aborting.",
                    applicationId);
                return;
            }

            var applicant = await unitOfWork.Applicants.GetByIdAsync(application.ApplicantId.Value);
            if (applicant == null)
            {
                _logger.LogWarning(
                    "DocumentVerificationAgent: Applicant not found for ApplicationId={ApplicationId}, aborting.",
                    applicationId);
                return;
            }

            var applicantJson = BuildApplicantJson(applicant);

            // ── Load all documents by ApplicantId (NOT ApplicationId) ─────
            var documents = (await unitOfWork.Documents.GetByApplicantIdAsync(application.ApplicantId.Value)).ToList();

            if (documents.Count == 0)
            {
                _logger.LogInformation(
                    "DocumentVerificationAgent: No documents found for ApplicantId={ApplicantId} (ApplicationId={ApplicationId}).",
                    application.ApplicantId.Value,
                    applicationId);
                return;
            }

            // Only verify documents that are still pending
            var pendingDocuments = documents
                .Where(d => string.Equals(d.VerificationResult, "pending", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (pendingDocuments.Count == 0)
            {
                _logger.LogInformation(
                    "DocumentVerificationAgent: No pending documents to verify for ApplicantId={ApplicantId} (ApplicationId={ApplicationId}).",
                    application.ApplicantId.Value,
                    applicationId);
                return;
            }

            _logger.LogInformation(
                "DocumentVerificationAgent: Verifying {Count} pending document(s) for ApplicantId={ApplicantId} (ApplicationId={ApplicationId})",
                pendingDocuments.Count,
                application.ApplicantId.Value,
                applicationId);

            // ── Verify each document, collect rejected details ────────────
            var rejectedNotes = new List<string>();

            foreach (var document in pendingDocuments)
            {
                await VerifySingleDocumentAsync(document, applicantJson, unitOfWork, applicationId);

                if (string.Equals(document.VerificationResult, "rejected", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(document.VerificationDetails))
                {
                    rejectedNotes.Add($"[{document.DocumentType ?? document.FileName}] {document.VerificationDetails}");
                }
            }

            // ── Set application status → "under_review" ───────────────────
            application.Status      = "under_review";
            application.LastUpdated = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await unitOfWork.Applications.UpdateAsync(application);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "DocumentVerificationAgent: ApplicationId={ApplicationId} status → under_review. Handing off to EligibilityEvaluationAgent.",
                applicationId);

            // ── Hand off to EligibilityEvaluationAgent ────────────────────
            // EligibilityAgent creates its own scope — no dependency on this scope
            await _eligibilityAgent.EvaluateAsync(applicationId, rejectedNotes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "DocumentVerificationAgent: Unhandled error for ApplicationId={ApplicationId}",
                applicationId);
        }
    }

    // ── Verify one document ───────────────────────────────────────────────────

    private async Task VerifySingleDocumentAsync(
        Document document,
        string applicantJson,
        IUnitOfWork unitOfWork,
        int applicationId)
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

            var fileBytes         = await DownloadBytesAsync(document.FilePath, document.FileName);
            var imageBase64List   = PrepareImagesFromBytes(fileBytes, document.FileName);
            var responseBody      = await CallOllamaAsync(imageBase64List, applicantJson, document);

            // Persist raw response to AgentLog
            await SaveAgentLogAsync(
                unitOfWork,
                applicationId,
                document.DocumentId,
                action: "document_verification",
                status: "llm_response",
                outputData: responseBody);

            var verificationResult = ParseLlmResponse(responseBody, document.DocumentId);

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

            // Persist error to AgentLog
            try
            {
                await SaveAgentLogAsync(
                    unitOfWork,
                    applicationId,
                    document.DocumentId,
                    action: "document_verification",
                    status: "error",
                    outputData: JsonSerializer.Serialize(new { error = ex.Message }, SerializerOptions));
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx,
                    "DocumentVerificationAgent: Failed to save AgentLog for error state (DocumentId={DocumentId})",
                    document.DocumentId);
            }

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

    private async Task SaveAgentLogAsync(
        IUnitOfWork unitOfWork,
        int applicationId,
        int documentId,
        string action,
        string status,
        string? outputData)
    {
        var log = new AgentLog
        {
            ApplicationId = applicationId,
            DocumentId = documentId,
            AgentType = nameof(DocumentVerificationAgent),
            Action = action,
            Status = status,
            OutputData = outputData,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        };

        await unitOfWork.AgentLogs.AddAsync(log);
    }

    // ── Step 1: Download bytes ────────────────────────────────────────────────

    private async Task<byte[]> DownloadBytesAsync(string url, string fileName)
    {
        using var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Failed to download '{fileName}' from Firebase Storage. HTTP {(int)response.StatusCode}",
                inner: null, statusCode: response.StatusCode);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        _logger.LogDebug("DocumentVerificationAgent: Downloaded '{FileName}' ({Size} bytes)", fileName, bytes.Length);
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
                new OllamaMessage { Role = "system", Content = DocumentVerificationAgentPrompts.Verification },
                new OllamaMessage { Role = "user",   Content = userPrompt, Images = imageBase64List }
            ]
        };

        var json = JsonSerializer.Serialize(requestBody, SerializerOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Ollama API error {(int)response.StatusCode}: {responseBody}",
                inner: null, statusCode: response.StatusCode);

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

            var result = string.Equals(llmResult.Result, "verified", StringComparison.OrdinalIgnoreCase)
                ? "verified" : "rejected";

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
            throw new InvalidOperationException($"Failed to parse LLM verification response: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

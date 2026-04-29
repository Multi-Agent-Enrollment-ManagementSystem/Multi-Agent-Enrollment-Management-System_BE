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
/// Eligibility Evaluation Agent — kiểm tra hồ sơ có đủ loại tài liệu theo admission type,
/// sau đó nhận xét chất lượng hồ sơ. Lưu kết quả vào Application.Notes và RequiresReview.
/// Được gọi nội bộ bởi DocumentVerificationAgent sau khi verification hoàn tất.
/// </summary>
public sealed class EligibilityEvaluationAgent : IEligibilityEvaluationAgent
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EligibilityEvaluationAgent> _logger;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _modelName;

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

    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png" };

    private static readonly HashSet<string> PdfExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf" };

    private readonly DocumentIntakeAgentPdfConverter _pdfConverter;

    public EligibilityEvaluationAgent(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EligibilityEvaluationAgent> logger)
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
    public async Task EvaluateAsync(int applicationId, List<string> verificationNotes)
    {
        _logger.LogInformation(
            "EligibilityEvaluationAgent: Starting evaluation for ApplicationId={ApplicationId}",
            applicationId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            // ── Load application ──────────────────────────────────────────
            var application = await unitOfWork.Applications.GetByIdAsync(applicationId);
            if (application == null)
            {
                _logger.LogWarning(
                    "EligibilityEvaluationAgent: ApplicationId={ApplicationId} not found, aborting.",
                    applicationId);
                return;
            }

            // ── Load admission type (required document list) ──────────────
            AdmissionType? admissionType = null;
            if (application.AdmissionTypeId.HasValue)
                admissionType = await unitOfWork.AdmissionTypes.GetByIdAsync(application.AdmissionTypeId.Value);

            var requiredDocTypes = ParseRequiredDocumentTypes(admissionType?.RequiredDocumentList);

            // ── Load applicant profile ────────────────────────────────────
            var applicant = application.ApplicantId.HasValue
                ? await unitOfWork.Applicants.GetByIdAsync(application.ApplicantId.Value)
                : null;

            var applicantJson = applicant != null
                ? BuildApplicantJson(applicant)
                : "{}";

            var eligibilityRules = admissionType?.EligibilityRules;
            var priorityRules = admissionType?.PriorityRules;

            // ── Load submitted & verified document types ──────────────────
            // Documents are associated with ApplicantId, not ApplicationId
            if (!application.ApplicantId.HasValue)
            {
                _logger.LogWarning(
                    "EligibilityEvaluationAgent: ApplicationId={ApplicationId} has no ApplicantId, cannot load documents.",
                    applicationId);
                return;
            }

            var documents = (await unitOfWork.Documents.GetByApplicantIdAsync(application.ApplicantId.Value)).ToList();
            var submittedDocTypes = documents
                .Where(d => !string.IsNullOrWhiteSpace(d.DocumentType))
                .Select(d => d.DocumentType!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Build evidence images from ALL submitted documents so LLM can perform
            // Step 1 completeness check by visually identifying document types.
            // Keep a small cap per PDF to avoid sending too many pages.
            var evidenceImages = await BuildEvidenceImagesAsync(documents);

            _logger.LogInformation(
                "EligibilityEvaluationAgent: Required={Required} | Submitted={Submitted} | EvidenceImages={EvidenceCount} for ApplicationId={ApplicationId}",
                string.Join(",", requiredDocTypes),
                string.Join(",", submittedDocTypes),
                evidenceImages.Count,
                applicationId);

            // ── Call LLM ──────────────────────────────────────────────────
            var responseBody = await CallOllamaAsync(
                requiredDocTypes,
                submittedDocTypes,
                applicantJson,
                evidenceImages,
                applicationId,
                eligibilityRules,
                priorityRules);

            // Save raw LLM response to AgentLog (application-level)
            await unitOfWork.AgentLogs.AddAsync(new AgentLog
            {
                ApplicationId = applicationId,
                DocumentId = null,
                AgentType = nameof(EligibilityEvaluationAgent),
                Action = "eligibility_evaluation",
                Status = "llm_response",
                OutputData = responseBody,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            });

            var eligibilityResult = ParseLlmResponse(responseBody, applicationId);

            // ── Determine if RequiresReview ───────────────────────────────
            var anyDocRejected = documents.Any(d =>
                string.Equals(d.VerificationResult, "rejected", StringComparison.OrdinalIgnoreCase));

            var eligibilityRejected = string.Equals(
                eligibilityResult.Result, "rejected", StringComparison.OrdinalIgnoreCase);

            var requiresReview = anyDocRejected || eligibilityRejected;

            // ── Build Notes (VerificationAgent details + EligibilityAgent details) ──
            var notesParts = new List<string>();

            if (verificationNotes.Count > 0)
            {
                notesParts.Add("[Document Verification]");
                notesParts.AddRange(verificationNotes);
            }

            // Only include eligibility evaluation notes when:
            // - the eligibility evaluation rejected the application, OR
            // - there are no rejected documents.
            // This avoids showing positive eligibility notes when the application still needs review due to rejected documents.
            var shouldIncludeEligibilityNotes = !anyDocRejected || eligibilityRejected;

            if (shouldIncludeEligibilityNotes)
            {
                if (!string.IsNullOrWhiteSpace(eligibilityResult.Level))
                {
                    notesParts.Add($"[Level]: {eligibilityResult.Level}");
                }
                if (!string.IsNullOrWhiteSpace(eligibilityResult.Details))
                {
                    notesParts.Add("[Eligibility Evaluation]");
                    notesParts.Add(eligibilityResult.Details);
                }
            }

            var notes = notesParts.Count > 0
                ? string.Join("\n", notesParts)
                : null;

            // ── Persist to Application ────────────────────────────────────
            application.RequiresReview = requiresReview;
            application.Notes         = notes;
            application.Level         = eligibilityResult.Level;
            application.LastUpdated   = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await unitOfWork.Applications.UpdateAsync(application);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "EligibilityEvaluationAgent: ApplicationId={ApplicationId} → Result={Result} | RequiresReview={RequiresReview}",
                applicationId, eligibilityResult.Result, requiresReview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EligibilityEvaluationAgent: Unhandled error for ApplicationId={ApplicationId}",
                applicationId);

            // Best-effort error log
            try
            {
                await unitOfWork.AgentLogs.AddAsync(new AgentLog
                {
                    ApplicationId = applicationId,
                    DocumentId = null,
                    AgentType = nameof(EligibilityEvaluationAgent),
                    Action = "eligibility_evaluation",
                    Status = "error",
                    OutputData = JsonSerializer.Serialize(new { error = ex.Message }, SerializerOptions),
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                });
            }
            catch
            {
                // ignore logging failures
            }
        }
    }

    // ── Call Ollama ───────────────────────────────────────────────────────────

    private async Task<string> CallOllamaAsync(
        List<string> requiredDocTypes,
        List<string> submittedDocTypes,
        string applicantJson,
        List<string> evidenceImagesBase64,
        int applicationId,
        string? eligibilityRules = null,
        string? priorityRules = null)
    {
        var rulesSection = "";
        if (!string.IsNullOrWhiteSpace(eligibilityRules) || !string.IsNullOrWhiteSpace(priorityRules))
        {
            rulesSection = "[RULES]\n";
            if (!string.IsNullOrWhiteSpace(eligibilityRules)) rulesSection += $"Eligibility Rules:\n{eligibilityRules}\n\n";
            if (!string.IsNullOrWhiteSpace(priorityRules)) rulesSection += $"Priority Rules:\n{priorityRules}\n\n";
        }

        var userPrompt =
            $"{rulesSection}" +
            $"[REQUIRED_DOCUMENT_TYPES]\n{string.Join(", ", requiredDocTypes.DefaultIfEmpty("(none specified)"))}\n\n" +
            $"[SUBMITTED_DOCUMENT_TYPES]\n{string.Join(", ", submittedDocTypes.DefaultIfEmpty("(none)"))}\n\n" +
            $"[APPLICANT_PROFILE]\n{applicantJson}\n\n" +
            "[EVIDENCE_DOCUMENTS]\n" +
            "Attached are images/pages from submitted certificates (schoolrank/graduation/achievement) for score verification.\n\n" +
            "Please evaluate the applicant's eligibility.";

        var requestBody = new OllamaChatRequest
        {
            Model = _modelName,
            Stream = false,
            Messages =
            [
                new OllamaMessage
                {
                    Role = "system",
                    Content = EligibilityEvaluationAgentPrompts.Evaluation
                },
                new OllamaMessage
                {
                    Role = "user",
                    Content = userPrompt,
                    Images = evidenceImagesBase64.Count > 0 ? evidenceImagesBase64 : null
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
            "EligibilityEvaluationAgent: Raw LLM response for ApplicationId={ApplicationId} — {Response}",
            applicationId, responseBody);

        return responseBody;
    }

    private async Task<List<string>> BuildEvidenceImagesAsync(List<Document> documents)
    {
        // Include ALL documents for visual completeness check.
        // We still require FilePath + FileName so we can download and detect file type.
        var candidateDocs = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.FilePath)
                        && !string.IsNullOrWhiteSpace(d.FileName))
            .ToList();

        var images = new List<string>();

        foreach (var doc in candidateDocs)
        {
            try
            {
                var fileBytes = await DownloadBytesAsync(doc.FilePath!, doc.FileName!);

                // Cap pages per PDF to avoid very large requests.
                var docImages = PrepareImagesFromBytes(fileBytes, doc.FileName!, maxImagesPerPdf: 3);
                images.AddRange(docImages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "EligibilityEvaluationAgent: Failed to load evidence document '{FileName}' (Type={Type}). Skipping.",
                    doc.FileName, doc.DocumentType);
            }
        }

        return images;
    }

    private async Task<byte[]> DownloadBytesAsync(string url, string fileName)
    {
        using var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Failed to download '{fileName}'. HTTP {(int)response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);

        return await response.Content.ReadAsByteArrayAsync();
    }

    private List<string> PrepareImagesFromBytes(byte[] fileBytes, string fileName, int? maxImagesPerPdf)
    {
        var ext = Path.GetExtension(fileName);

        if (ImageExtensions.Contains(ext))
            return [Convert.ToBase64String(fileBytes)];

        if (PdfExtensions.Contains(ext))
        {
            var all = _pdfConverter.Convert(fileBytes, fileName);
            return maxImagesPerPdf.HasValue
                ? all.Take(Math.Max(0, maxImagesPerPdf.Value)).ToList()
                : all;
        }

        throw new NotSupportedException(
            $"File type '{ext}' is not supported. Allowed: " +
            string.Join(", ", ImageExtensions.Concat(PdfExtensions)));
    }

    // ── Parse LLM response ────────────────────────────────────────────────────

    private EligibilityEvaluationResult ParseLlmResponse(string responseBody, int applicationId)
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

            var llmResult = JsonSerializer.Deserialize<LlmEligibilityResponse>(content, DeserializerOptions)
                ?? throw new InvalidOperationException("LLM inner JSON could not be deserialized.");

            var result = string.Equals(llmResult.Result, "passed", StringComparison.OrdinalIgnoreCase)
                ? "passed"
                : "rejected";

            return new EligibilityEvaluationResult
            {
                Result  = result,
                Level   = llmResult.Level,
                Details = llmResult.Details
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "EligibilityEvaluationAgent: Failed to parse LLM response for ApplicationId={ApplicationId}. Body: {Body}",
                applicationId, responseBody);

            throw new InvalidOperationException(
                $"Failed to parse LLM eligibility response: {ex.Message}", ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parse RequiredDocumentList string (comma-separated hoặc JSON array) thành List&lt;string&gt;.
    /// </summary>
    private static List<string> ParseRequiredDocumentTypes(string? requiredDocumentList)
    {
        if (string.IsNullOrWhiteSpace(requiredDocumentList))
            return [];

        var trimmed = requiredDocumentList.Trim();

        // Thử parse JSON array trước
        if (trimmed.StartsWith('['))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (parsed != null)
                    return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            catch { /* fall through to comma-split */ }
        }

        // Comma-separated fallback
        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string BuildApplicantJson(MAEMS.Domain.Entities.Applicant applicant)
    {
        var data = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["full_name"]            = applicant.FullName,
            ["date_of_birth"]        = applicant.DateOfBirth?.ToString("yyyy-MM-dd"),
            ["gender"]               = applicant.Gender,
            ["high_school_name"]     = applicant.HighSchoolName,
            ["high_school_province"] = applicant.HighSchoolProvince,
            ["graduation_year"]      = applicant.GraduationYear
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

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
    private readonly IOpenAIService _openAIService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EligibilityEvaluationAgent> _logger;
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
        IOpenAIService openAIService,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EligibilityEvaluationAgent> logger)
    {
        _openAIService = openAIService;
        _scopeFactory = scopeFactory;
        _logger = logger;

        _pdfConverter = new DocumentIntakeAgentPdfConverter(logger);
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

            // ── Update applicant scores if available ──────────────────────
            var llmResponseObj = eligibilityResult.RawLlmResponse;
            if (llmResponseObj != null && application.ApplicantId.HasValue)
            {
                var currentScore = await unitOfWork.Scores.GetByApplicantIdAsync(application.ApplicantId.Value);
                if (currentScore == null)
                {
                    currentScore = new MAEMS.Domain.Entities.Score { ApplicantId = application.ApplicantId.Value };
                    UpdateScoreEntity(currentScore, llmResponseObj);
                    await unitOfWork.Scores.AddAsync(currentScore);
                }
                else
                {
                    UpdateScoreEntity(currentScore, llmResponseObj);
                    await unitOfWork.Scores.UpdateAsync(currentScore);
                }
            }

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

        string responseBody;
        if (evidenceImagesBase64 != null && evidenceImagesBase64.Count > 0)
        {
            responseBody = await _openAIService.GetVisionCompletionAsync(
                systemPrompt: EligibilityEvaluationAgentPrompts.Evaluation,
                userMessage: userPrompt,
                base64Images: evidenceImagesBase64,
                maxTokens: 2000);
        }
        else
        {
            responseBody = await _openAIService.GetChatCompletionAsync(
                systemPrompt: EligibilityEvaluationAgentPrompts.Evaluation,
                userMessage: userPrompt,
                maxTokens: 2000);
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
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url);

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
            var content = StripMarkdownFences(responseBody);

            var llmResult = JsonSerializer.Deserialize<LlmEligibilityResponse>(content, DeserializerOptions)
                ?? throw new InvalidOperationException("LLM inner JSON could not be deserialized.");

            var result = string.Equals(llmResult.Result, "passed", StringComparison.OrdinalIgnoreCase)
                ? "passed"
                : "rejected";

            return new EligibilityEvaluationResult
            {
                Result  = result,
                Level   = llmResult.Level,
                Details = llmResult.Details,
                RawLlmResponse = new MAEMS.Application.DTOs.Agent.LlmEligibilityResponseDto
                {
                    Result = llmResult.Result,
                    Level = llmResult.Level,
                    Details = llmResult.Details,
                    Hk2Math = llmResult.Hk2Math,
                    Hk2Literature = llmResult.Hk2Literature,
                    Hk2ForeignLanguage = llmResult.Hk2ForeignLanguage,
                    Hk2History = llmResult.Hk2History,
                    Hk2Physics = llmResult.Hk2Physics,
                    Hk2Chemistry = llmResult.Hk2Chemistry,
                    Hk2Biology = llmResult.Hk2Biology,
                    Hk2Geography = llmResult.Hk2Geography,
                    Hk2EconomicsLaw = llmResult.Hk2EconomicsLaw,
                    Hk2Informatics = llmResult.Hk2Informatics,
                    Hk2Technology = llmResult.Hk2Technology,
                    ThptMath = llmResult.ThptMath,
                    ThptLiterature = llmResult.ThptLiterature,
                    ThptForeignLanguage = llmResult.ThptForeignLanguage,
                    ThptHistory = llmResult.ThptHistory,
                    ThptGeography = llmResult.ThptGeography,
                    ThptPhysics = llmResult.ThptPhysics,
                    ThptChemistry = llmResult.ThptChemistry,
                    ThptBiology = llmResult.ThptBiology,
                    ThptEconomicsLaw = llmResult.ThptEconomicsLaw,
                    ThptInformatics = llmResult.ThptInformatics,
                    ThptTechnology = llmResult.ThptTechnology,
                    Dgnl = llmResult.Dgnl
                }
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

    private static void UpdateScoreEntity(MAEMS.Domain.Entities.Score score, MAEMS.Application.DTOs.Agent.LlmEligibilityResponseDto llm)
    {
        if (llm.Hk2Math.HasValue) score.Hk2Math = llm.Hk2Math;
        if (llm.Hk2Literature.HasValue) score.Hk2Literature = llm.Hk2Literature;
        if (llm.Hk2ForeignLanguage.HasValue) score.Hk2ForeignLanguage = llm.Hk2ForeignLanguage;
        if (llm.Hk2History.HasValue) score.Hk2History = llm.Hk2History;
        if (llm.Hk2Physics.HasValue) score.Hk2Physics = llm.Hk2Physics;
        if (llm.Hk2Chemistry.HasValue) score.Hk2Chemistry = llm.Hk2Chemistry;
        if (llm.Hk2Biology.HasValue) score.Hk2Biology = llm.Hk2Biology;
        if (llm.Hk2Geography.HasValue) score.Hk2Geography = llm.Hk2Geography;
        if (llm.Hk2EconomicsLaw.HasValue) score.Hk2EconomicsLaw = llm.Hk2EconomicsLaw;
        if (llm.Hk2Informatics.HasValue) score.Hk2Informatics = llm.Hk2Informatics;
        if (llm.Hk2Technology.HasValue) score.Hk2Technology = llm.Hk2Technology;

        if (llm.ThptMath.HasValue) score.ThptMath = llm.ThptMath;
        if (llm.ThptLiterature.HasValue) score.ThptLiterature = llm.ThptLiterature;
        if (llm.ThptForeignLanguage.HasValue) score.ThptForeignLanguage = llm.ThptForeignLanguage;
        if (llm.ThptHistory.HasValue) score.ThptHistory = llm.ThptHistory;
        if (llm.ThptGeography.HasValue) score.ThptGeography = llm.ThptGeography;
        if (llm.ThptPhysics.HasValue) score.ThptPhysics = llm.ThptPhysics;
        if (llm.ThptChemistry.HasValue) score.ThptChemistry = llm.ThptChemistry;
        if (llm.ThptBiology.HasValue) score.ThptBiology = llm.ThptBiology;
        if (llm.ThptEconomicsLaw.HasValue) score.ThptEconomicsLaw = llm.ThptEconomicsLaw;
        if (llm.ThptInformatics.HasValue) score.ThptInformatics = llm.ThptInformatics;
        if (llm.ThptTechnology.HasValue) score.ThptTechnology = llm.ThptTechnology;

        if (llm.Dgnl.HasValue) score.Dgnl = llm.Dgnl;
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

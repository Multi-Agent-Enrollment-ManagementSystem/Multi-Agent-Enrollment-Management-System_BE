using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MAEMS.Application.DTOs.MajorAdvisor;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Major Advisor Agent - Analyzes academic documents (transcript or competency test)
/// and recommends suitable university majors. Logs all analysis to AgentLog for QA review.
/// </summary>
public sealed class MajorAdvisorAgent : IMajorAdvisorAgent
{
    private readonly HttpClient _httpClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MajorAdvisorAgent> _logger;
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

    public MajorAdvisorAgent(
        HttpClient httpClient,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<MajorAdvisorAgent> logger)
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
    public async Task<MajorAdvisorResult> AnalyzeAndRecommendAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var result = new MajorAdvisorResult();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "MajorAdvisorAgent: Starting analysis for file '{FileName}' ({Size} bytes)",
                file.FileName, file.Length);

            // Step 1: Prepare images
            var images = await PrepareImagesAsync(file, cancellationToken);

            // Step 2: Detect document type
            var docType = await DetectDocumentTypeAsync(images.First(), file.FileName, cancellationToken);
            result.DetectedDocumentType = docType.Type;

            if (docType.Type == DocumentType.Unknown)
            {
                result.Success = false;
                result.ErrorMessage = "Không thể xác định loại tài liệu. Vui lòng tải lên học bạ THPT hoặc kết quả thi ĐGNL.";
                await LogToAgentLogAsync(result, "failed", startTime, cancellationToken);
                return result;
            }

            // Step 3: Extract scores based on document type
            var scores = await ExtractScoresAsync(images, docType.Type, file.FileName, cancellationToken);
            result.Scores = scores;

            // Step 4: Load majors from database
            var majors = await LoadMajorsAsync(cancellationToken);

            if (majors.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "Không tìm thấy danh sách ngành học trong hệ thống.";
                await LogToAgentLogAsync(result, "failed", startTime, cancellationToken);
                return result;
            }

            // Step 4.5: Filter relevant majors based on scores
            var relevantMajors = FilterRelevantMajors(majors, docType.Type, scores);

            if (relevantMajors.Count == 0)
            {
                result.Success = false;
                result.ErrorMessage = "Không tìm thấy ngành phù hợp với điểm số của bạn.";
                await LogToAgentLogAsync(result, "failed", startTime, cancellationToken);
                return result;
            }

            // Step 5: Get major recommendations
            var recommendations = await GetRecommendationsAsync(
                docType.Type,
                scores,
                relevantMajors,
                file.FileName,
                cancellationToken);

            result.Recommendations = recommendations;
            result.Success = true;

            _logger.LogInformation(
                "MajorAdvisorAgent: Analysis completed for '{FileName}' - {Count} recommendations generated",
                file.FileName, recommendations.Count);

            // Step 6: Log to AgentLog for QA review
            await LogToAgentLogAsync(result, "success", startTime, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MajorAdvisorAgent: Error analyzing '{FileName}'", file.FileName);

            result.Success = false;
            result.ErrorMessage = $"Lỗi khi phân tích tài liệu: {ex.Message}";

            await LogToAgentLogAsync(result, "failed", startTime, cancellationToken);

            return result;
        }
    }

    // ── Step 1: Prepare images (reuse PdfConverter) ──────────────────────────

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
            return [Convert.ToBase64String(fileBytes)];
        }

        if (PdfExtensions.Contains(ext))
        {
            var pages = _pdfConverter.Convert(fileBytes, file.FileName);
            _logger.LogInformation(
                "MajorAdvisorAgent: Converted PDF '{FileName}' to {Pages} images",
                file.FileName, pages.Count);
            return pages;
        }

        throw new NotSupportedException(
            $"Định dạng file '{ext}' không được hỗ trợ. Chỉ chấp nhận: " +
            string.Join(", ", ImageExtensions.Concat(PdfExtensions)));
    }

    // ── Step 2: Detect document type ─────────────────────────────────────────

    private async Task<DocumentTypeResult> DetectDocumentTypeAsync(
        string imageBase64,
        string fileName,
        CancellationToken cancellationToken)
    {
        var requestBody = new OllamaChatRequest
        {
            Model = _modelName,
            Stream = false,
            Messages =
            [
                new OllamaMessage
                {
                    Role = "user",
                    Content = MajorAdvisorAgentPrompts.DocumentTypeDetection,
                    Images = [imageBase64]
                }
            ]
        };

        var responseBody = await CallOllamaAsync(requestBody, fileName, cancellationToken);
        var ollamaResponse = ParseOllamaResponse<OllamaDocTypeResponse>(responseBody, fileName);

        var docType = ollamaResponse.Type.ToLowerInvariant() switch
        {
            "transcript" => DocumentType.Transcript,
            "competency_test" => DocumentType.CompetencyTest,
            _ => DocumentType.Unknown
        };

        _logger.LogInformation(
            "MajorAdvisorAgent: Document type detected as '{Type}' (confidence: {Confidence:F2})",
            docType, ollamaResponse.Confidence);

        return new DocumentTypeResult
        {
            Type = docType,
            Confidence = ollamaResponse.Confidence
        };
    }

    // ── Step 3: Extract scores ────────────────────────────────────────────────

    private async Task<ExtractedScores> ExtractScoresAsync(
        List<string> images,
        DocumentType docType,
        string fileName,
        CancellationToken cancellationToken)
    {
        var scores = new ExtractedScores();

        if (docType == DocumentType.Transcript)
        {
            var requestBody = new OllamaChatRequest
            {
                Model = _modelName,
                Stream = false,
                Messages =
                [
                    new OllamaMessage
                    {
                        Role = "user",
                        Content = MajorAdvisorAgentPrompts.TranscriptScoreExtraction,
                        Images = images
                    }
                ]
            };

            var responseBody = await CallOllamaAsync(requestBody, fileName, cancellationToken);
            var ollamaResponse = ParseOllamaResponse<OllamaTranscriptResponse>(responseBody, fileName);

            if (!ollamaResponse.Success)
            {
                throw new InvalidOperationException($"Không thể đọc điểm từ học bạ: {ollamaResponse.ErrorMessage}");
            }

            scores.Transcript = MapTranscriptScores(ollamaResponse);
        }
        else if (docType == DocumentType.CompetencyTest)
        {
            var requestBody = new OllamaChatRequest
            {
                Model = _modelName,
                Stream = false,
                Messages =
                [
                    new OllamaMessage
                    {
                        Role = "user",
                        Content = MajorAdvisorAgentPrompts.CompetencyScoreExtraction,
                        Images = images
                    }
                ]
            };

            var responseBody = await CallOllamaAsync(requestBody, fileName, cancellationToken);
            var ollamaResponse = ParseOllamaResponse<OllamaCompetencyResponse>(responseBody, fileName);

            if (!ollamaResponse.Success)
            {
                throw new InvalidOperationException($"Không thể đọc điểm từ kết quả ĐGNL: {ollamaResponse.ErrorMessage}");
            }

            scores.Competency = new CompetencyData
            {
                TotalScore = ollamaResponse.TotalScore,
                TiengViet = ollamaResponse.TiengViet,
                TiengAnh = ollamaResponse.TiengAnh,
                ToanHoc = ollamaResponse.ToanHoc,
                TuDuyKhoaHoc = ollamaResponse.TuDuyKhoaHoc,
                PercentileRange = ollamaResponse.PercentileRange
            };
        }

        return scores;
    }

    private TranscriptData MapTranscriptScores(OllamaTranscriptResponse response)
    {
        var data = new TranscriptData();

        if (response.Grade11 != null)
        {
            data.Grade11_Toan = response.Grade11.Toan;
            data.Grade11_NguVan = response.Grade11.NguVan;
            data.Grade11_NgoaiNgu = response.Grade11.NgoaiNgu;
            data.Grade11_VatLy = response.Grade11.VatLy;
            data.Grade11_HoaHoc = response.Grade11.HoaHoc;
            data.Grade11_SinhHoc = response.Grade11.SinhHoc;
            data.Grade11_LichSu = response.Grade11.LichSu;
            data.Grade11_DiaLy = response.Grade11.DiaLy;
            data.Grade11_GDCD = response.Grade11.GDCD;
        }

        if (response.Grade12 != null)
        {
            data.Grade12_Toan = response.Grade12.Toan;
            data.Grade12_NguVan = response.Grade12.NguVan;
            data.Grade12_NgoaiNgu = response.Grade12.NgoaiNgu;
            data.Grade12_VatLy = response.Grade12.VatLy;
            data.Grade12_HoaHoc = response.Grade12.HoaHoc;
            data.Grade12_SinhHoc = response.Grade12.SinhHoc;
            data.Grade12_LichSu = response.Grade12.LichSu;
            data.Grade12_DiaLy = response.Grade12.DiaLy;
            data.Grade12_GDCD = response.Grade12.GDCD;
        }

        // Calculate average GPA
        var allScores = new List<decimal?>();
        if (response.Grade11 != null)
        {
            allScores.AddRange(new[]
            {
                response.Grade11.Toan, response.Grade11.NguVan, response.Grade11.NgoaiNgu,
                response.Grade11.VatLy, response.Grade11.HoaHoc, response.Grade11.SinhHoc,
                response.Grade11.LichSu, response.Grade11.DiaLy, response.Grade11.GDCD
            });
        }
        if (response.Grade12 != null)
        {
            allScores.AddRange(new[]
            {
                response.Grade12.Toan, response.Grade12.NguVan, response.Grade12.NgoaiNgu,
                response.Grade12.VatLy, response.Grade12.HoaHoc, response.Grade12.SinhHoc,
                response.Grade12.LichSu, response.Grade12.DiaLy, response.Grade12.GDCD
            });
        }

        var validScores = allScores.Where(s => s.HasValue).Select(s => s!.Value).ToList();
        if (validScores.Any())
        {
            data.AverageGpa = Math.Round(validScores.Average(), 2);
        }

        return data;
    }

    // ── Step 4: Load majors from database ─────────────────────────────────────

    private async Task<List<Major>> LoadMajorsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var majors = await unitOfWork.Majors.GetAllAsync();
        var activeMajors = majors.Where(m => m.IsActive == true).ToList();

        _logger.LogInformation(
            "MajorAdvisorAgent: Loaded {Count} active majors from database (will filter by relevance before LLM)",
            activeMajors.Count);

        return activeMajors;
    }

    // ── Helper: Filter relevant majors based on scores ───────────────────────

    private List<Major> FilterRelevantMajors(List<Major> allMajors, DocumentType docType, ExtractedScores scores)
    {
        const int MaxMajorsForLlm = 20; // Limit to prevent LLM overload

        // For competency test: filter by total score threshold
        if (docType == DocumentType.CompetencyTest && scores.Competency != null)
        {
            var totalScore = scores.Competency.TotalScore;

            // Categorize majors by competitiveness (simplified heuristic)
            var relevantMajors = allMajors
                .Where(m =>
                {
                    var code = m.MajorCode?.ToUpperInvariant() ?? "";

                    // High-demand STEM majors need ≥700
                    if ((code.Contains("CNTT") || code.Contains("KTPM") || code == "Y" || code == "DUOC") 
                        && totalScore < 700)
                        return false;

                    // Standard majors accessible at ≥500
                    if (totalScore < 500)
                        return false;

                    return true;
                })
                .Take(MaxMajorsForLlm)
                .ToList();

            _logger.LogInformation(
                "MajorAdvisorAgent: Filtered to {Count} majors for ĐGNL score {Score}",
                relevantMajors.Count, totalScore);

            return relevantMajors;
        }

        // For transcript: filter by subject strength (simplified - take top majors)
        if (docType == DocumentType.Transcript && scores.Transcript != null)
        {
            var transcript = scores.Transcript;
            var strongSubjects = new List<string>();

            // Identify strong subjects (≥8.0)
            if (transcript.Grade12_Toan >= 8.0m) strongSubjects.Add("Toán");
            if (transcript.Grade12_VatLy >= 8.0m) strongSubjects.Add("Lý");
            if (transcript.Grade12_HoaHoc >= 8.0m) strongSubjects.Add("Hóa");
            if (transcript.Grade12_SinhHoc >= 8.0m) strongSubjects.Add("Sinh");
            if (transcript.Grade12_NguVan >= 8.0m) strongSubjects.Add("Văn");
            if (transcript.Grade12_LichSu >= 8.0m) strongSubjects.Add("Sử");
            if (transcript.Grade12_DiaLy >= 8.0m) strongSubjects.Add("Địa");
            if (transcript.Grade12_NgoaiNgu >= 8.0m) strongSubjects.Add("Anh");

            // Simple heuristic: if strong in STEM subjects, prioritize STEM majors
            var hasStem = strongSubjects.Any(s => s == "Toán" || s == "Lý" || s == "Hóa");
            var hasHumanities = strongSubjects.Any(s => s == "Văn" || s == "Sử" || s == "Địa");

            var relevantMajors = allMajors
                .Where(m =>
                {
                    var code = m.MajorCode?.ToUpperInvariant() ?? "";
                    var name = m.MajorName?.ToLowerInvariant() ?? "";

                    // STEM majors if strong in math/science
                    if (hasStem && (code.Contains("CNTT") || code.Contains("KT") || name.Contains("kỹ thuật") 
                        || name.Contains("công nghệ")))
                        return true;

                    // Humanities/Business if strong in language/social
                    if (hasHumanities && (name.Contains("quản trị") || name.Contains("kinh tế") 
                        || name.Contains("ngôn ngữ") || name.Contains("du lịch")))
                        return true;

                    // Include balanced majors for all students
                    if (name.Contains("quản trị kinh doanh") || name.Contains("marketing"))
                        return true;

                    return false;
                })
                .Take(MaxMajorsForLlm)
                .ToList();

            // Fallback: if filtering is too aggressive, take top 20 by alphabetical
            if (relevantMajors.Count < 10)
            {
                relevantMajors = allMajors.Take(MaxMajorsForLlm).ToList();
            }

            _logger.LogInformation(
                "MajorAdvisorAgent: Filtered to {Count} majors for transcript (strong subjects: {Subjects})",
                relevantMajors.Count, string.Join(", ", strongSubjects));

            return relevantMajors;
        }

        // Fallback: return top N majors
        return allMajors.Take(MaxMajorsForLlm).ToList();
    }

    // ── Step 5: Get major recommendations ─────────────────────────────────────

    private async Task<List<MajorRecommendation>> GetRecommendationsAsync(
        DocumentType docType,
        ExtractedScores scores,
        List<Major> majors,
        string fileName,
        CancellationToken cancellationToken)
    {
        var docTypeStr = docType == DocumentType.Transcript ? "transcript" : "competency_test";
        var scoresJson = JsonSerializer.Serialize(scores, ResponseDeserializerOptions);
        var majorsJson = JsonSerializer.Serialize(majors.Select(m => new
        {
            m.MajorCode,
            m.MajorName,
            m.Description
        }), ResponseDeserializerOptions);

        var prompt = $"""
            [DOCUMENT_TYPE]: {docTypeStr}

            [SCORES]: 
            {scoresJson}

            [MAJORS]:
            {majorsJson}

            {MajorAdvisorAgentPrompts.MajorRecommendation}
            """;

        var requestBody = new OllamaChatRequest
        {
            Model = _modelName,
            Stream = false,
            Messages =
            [
                new OllamaMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        var responseBody = await CallOllamaAsync(requestBody, fileName, cancellationToken);
        var recommendations = ParseOllamaResponse<List<OllamaRecommendationResponse>>(responseBody, fileName);

        return recommendations.Select(r => new MajorRecommendation
        {
            MajorCode = r.MajorCode,
            MajorName = r.MajorName,
            MatchScore = r.MatchScore,
            Reasoning = r.Reasoning,
            Strengths = r.Strengths,
            Concerns = r.Concerns,
            AdmissionMethod = r.AdmissionMethod
        }).ToList();
    }

    // ── Step 6: Log to AgentLog ───────────────────────────────────────────────

    private async Task LogToAgentLogAsync(
        MajorAdvisorResult result,
        string status,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var outputData = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var log = new AgentLog
            {
                ApplicationId = null, // No application - public service
                DocumentId = null,    // No document entity - file not stored
                AgentType = "MajorAdvisor",
                Action = "AnalyzeDocument",
                Status = status,
                OutputData = outputData,
                CreatedAt = DateTime.Now // Use local time for PostgreSQL timestamp without time zone
            };

            await unitOfWork.AgentLogs.AddAsync(log);
            await unitOfWork.SaveChangesAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "MajorAdvisorAgent: Logged to AgentLog (LogId={LogId}, Status={Status}, Duration={Duration}ms)",
                log.LogId, status, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MajorAdvisorAgent: Failed to log to AgentLog");
            // Don't throw - logging failure shouldn't break the main flow
        }
    }

    // ── Helper: Call Ollama API ───────────────────────────────────────────────

    private async Task<string> CallOllamaAsync(
        OllamaChatRequest requestBody,
        string fileName,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestBody, RequestSerializerOptions);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "MajorAdvisorAgent: Ollama returned HTTP {StatusCode} for '{FileName}'. Body: {Body}",
                (int)response.StatusCode, fileName, responseBody);

            throw new HttpRequestException(
                $"Ollama API error {(int)response.StatusCode}: {responseBody}");
        }

        return responseBody;
    }

    // ── Helper: Parse Ollama response ─────────────────────────────────────────

    private T ParseOllamaResponse<T>(string responseBody, string fileName)
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

            var result = JsonSerializer.Deserialize<T>(content, ResponseDeserializerOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize to {typeof(T).Name}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MajorAdvisorAgent: Failed to parse Ollama response for '{FileName}'. Body: {Body}",
                fileName, responseBody);
            throw;
        }
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
        return trimmed;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Internal Ollama Response Models (not exposed to API layer)
// ═══════════════════════════════════════════════════════════════════════════

internal sealed class OllamaDocTypeResponse
{
    public string Type { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

internal sealed class OllamaTranscriptResponse
{
    public bool Success { get; set; }
    public TranscriptGrades? Grade11 { get; set; }
    public TranscriptGrades? Grade12 { get; set; }
    public string? ErrorMessage { get; set; }
}

internal sealed class TranscriptGrades
{
    public decimal? Toan { get; set; }
    public decimal? NguVan { get; set; }
    public decimal? NgoaiNgu { get; set; }
    public decimal? VatLy { get; set; }
    public decimal? HoaHoc { get; set; }
    public decimal? SinhHoc { get; set; }
    public decimal? LichSu { get; set; }
    public decimal? DiaLy { get; set; }
    public decimal? GDCD { get; set; }
}

internal sealed class OllamaCompetencyResponse
{
    public bool Success { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? TiengViet { get; set; }
    public decimal? TiengAnh { get; set; }
    public decimal? ToanHoc { get; set; }
    public decimal? TuDuyKhoaHoc { get; set; }
    public string? PercentileRange { get; set; }
    public string? ErrorMessage { get; set; }
}

internal sealed class OllamaRecommendationResponse
{
    public string MajorCode { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
    public string AdmissionMethod { get; set; } = string.Empty;
}


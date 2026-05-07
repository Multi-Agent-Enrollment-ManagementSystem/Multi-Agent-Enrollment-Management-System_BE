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
        var result = new MajorAdvisorResult
        {
            RawOllamaResponses = new Dictionary<string, string>()
        };
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "MajorAdvisorAgent: Starting analysis for file '{FileName}' ({Size} bytes)",
                file.FileName, file.Length);

            // Step 1: Prepare images
            var images = await PrepareImagesAsync(file, cancellationToken);

            // Step 2: Detect document type
            var (docType, docTypeRaw) = await DetectDocumentTypeAsync(images.First(), file.FileName, cancellationToken);
            result.DetectedDocumentType = docType.Type;
            result.RawOllamaResponses["document_type_detection"] = docTypeRaw;

            if (docType.Type == DocumentType.Unknown)
            {
                result.Result = "failed";
                result.ErrorMessage = "Không thể xác định loại tài liệu. Vui lòng tải lên học bạ THPT hoặc kết quả thi ĐGNL.";
                await LogToAgentLogAsync(result, startTime, cancellationToken);
                return result;
            }

            // Step 3: Extract scores based on document type
            var (scores, scoresRaw) = await ExtractScoresAsync(images, docType.Type, file.FileName, cancellationToken);
            result.Scores = scores;
            result.RawOllamaResponses["score_extraction"] = scoresRaw;

            // Step 4: Load programs from database
            var programs = await LoadProgramsAsync(cancellationToken);

            if (programs.Count == 0)
            {
                result.Result = "failed";
                result.ErrorMessage = "Không tìm thấy danh sách chương trình đào tạo trong hệ thống.";
                await LogToAgentLogAsync(result, startTime, cancellationToken);
                return result;
            }

            // Step 4.5: Filter relevant programs based on scores
            var relevantPrograms = FilterRelevantPrograms(programs, docType.Type, scores);

            if (relevantPrograms.Count == 0)
            {
                result.Result = "failed";
                result.ErrorMessage = "Không tìm thấy chương trình phù hợp với điểm số của bạn.";
                await LogToAgentLogAsync(result, startTime, cancellationToken);
                return result;
            }

            // Step 5: Get program recommendations
            var (recommendations, recommendRaw) = await GetRecommendationsAsync(
                docType.Type,
                scores,
                relevantPrograms,
                file.FileName,
                cancellationToken);

            result.Recommendations = recommendations;
            result.RawOllamaResponses["program_recommendation"] = recommendRaw;
            result.Result = "passed";

            // Step 5.5: Build summary for QA
            var summaryParts = new List<string>
            {
                $"Document Type: {docType.Type}",
                $"Programs Recommended: {recommendations.Count}",
                $"Top Match: {recommendations.FirstOrDefault()?.ProgramName ?? "N/A"} ({recommendations.FirstOrDefault()?.MatchScore ?? 0}/100)"
            };

            if (scores.Transcript != null)
            {
                summaryParts.Add($"GPA: {scores.Transcript.AverageGpa:F2}");
            }
            else if (scores.Competency != null)
            {
                summaryParts.Add($"ĐGNL Score: {scores.Competency.TotalScore}/1200");
            }
            else if (scores.SchoolRank != null)
            {
                summaryParts.Add($"SchoolRank: Top{scores.SchoolRank.Rank}, Score: {scores.SchoolRank.Grade12Score}");
            }

            result.RawOllamaResponses["summary"] = string.Join(" | ", summaryParts);

            _logger.LogInformation(
                "MajorAdvisorAgent: Analysis completed for '{FileName}' - {Count} recommendations generated",
                file.FileName, recommendations.Count);

            // Step 6: Log to AgentLog for QA review
            await LogToAgentLogAsync(result, startTime, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MajorAdvisorAgent: Error analyzing '{FileName}'", file.FileName);

            result.Result = "failed";
            result.ErrorMessage = $"Lỗi khi phân tích tài liệu: {ex.Message}";

            await LogToAgentLogAsync(result, startTime, cancellationToken);

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

    private async Task<(DocumentTypeResult Result, string RawResponse)> DetectDocumentTypeAsync(
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
            "schoolrank" => DocumentType.SchoolRank,
            _ => DocumentType.Unknown
        };

        _logger.LogInformation(
            "MajorAdvisorAgent: Document type detected as '{Type}' (confidence: {Confidence:F2})",
            docType, ollamaResponse.Confidence);

        var result = new DocumentTypeResult
        {
            Type = docType,
            Confidence = ollamaResponse.Confidence
        };

        return (result, responseBody);
    }

    // ── Step 3: Extract scores ────────────────────────────────────────────────

    private async Task<(ExtractedScores Scores, string RawResponse)> ExtractScoresAsync(
        List<string> images,
        DocumentType docType,
        string fileName,
        CancellationToken cancellationToken)
    {
        var scores = new ExtractedScores();
        string rawResponse;

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

            rawResponse = await CallOllamaAsync(requestBody, fileName, cancellationToken);
            var ollamaResponse = ParseOllamaResponse<OllamaTranscriptResponse>(rawResponse, fileName);

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

            rawResponse = await CallOllamaAsync(requestBody, fileName, cancellationToken);
            var ollamaResponse = ParseOllamaResponse<OllamaCompetencyResponse>(rawResponse, fileName);

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
        else if (docType == DocumentType.SchoolRank)
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
                        Content = MajorAdvisorAgentPrompts.SchoolRankScoreExtraction,
                        Images = images
                    }
                ]
            };

            rawResponse = await CallOllamaAsync(requestBody, fileName, cancellationToken);
            var ollamaResponse = ParseOllamaResponse<OllamaSchoolRankResponse>(rawResponse, fileName);

            if (!ollamaResponse.Success)
            {
                throw new InvalidOperationException($"Không thể đọc thông tin từ chứng nhận SchoolRank: {ollamaResponse.ErrorMessage}");
            }

            scores.SchoolRank = new SchoolRankData
            {
                Rank = ollamaResponse.Rank,
                Grade12Score = ollamaResponse.Grade12Score,
                StudentName = ollamaResponse.StudentName,
                SchoolName = ollamaResponse.SchoolName,
                Year = ollamaResponse.Year
            };
        }
        else
        {
            rawResponse = "{}"; // Unknown document type
        }

        return (scores, rawResponse);
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

    // ── Step 4: Load programs from database ─────────────────────────────────────

    private async Task<List<Program>> LoadProgramsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var programs = await unitOfWork.Programs.GetAllAsync();
        var activePrograms = programs.Where(p => p.IsActive == true).ToList();

        _logger.LogInformation(
            "MajorAdvisorAgent: Loaded {Count} active programs from database (will filter by relevance before LLM)",
            activePrograms.Count);

        return activePrograms;
    }

    // ── Helper: Filter relevant programs based on scores ───────────────────────

    private List<Program> FilterRelevantPrograms(List<Program> allPrograms, DocumentType docType, ExtractedScores scores)
    {
        const int MaxProgramsForLlm = 20; // Limit to prevent LLM overload

        // For competency test: filter by total score threshold
        if (docType == DocumentType.CompetencyTest && scores.Competency != null)
        {
            var totalScore = scores.Competency.TotalScore;

            // Categorize programs by competitiveness (simplified heuristic)
            var relevantPrograms = allPrograms
                .Where(p =>
                {
                    var name = p.ProgramName?.ToLowerInvariant() ?? "";

                    // High-demand STEM programs need ≥700
                    if ((name.Contains("công nghệ thông tin") || name.Contains("khoa học máy tính") 
                        || name.Contains("y") || name.Contains("dược")) 
                        && totalScore < 700)
                        return false;

                    // Standard programs accessible at ≥500
                    if (totalScore < 500)
                        return false;

                    return true;
                })
                .Take(MaxProgramsForLlm)
                .ToList();

            _logger.LogInformation(
                "MajorAdvisorAgent: Filtered to {Count} programs for ĐGNL score {Score}",
                relevantPrograms.Count, totalScore);

            return relevantPrograms;
        }

        // For transcript: filter by subject strength (simplified - take top programs)
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

            // Simple heuristic: if strong in STEM subjects, prioritize STEM programs
            var hasStem = strongSubjects.Any(s => s == "Toán" || s == "Lý" || s == "Hóa");
            var hasHumanities = strongSubjects.Any(s => s == "Văn" || s == "Sử" || s == "Địa");

            var relevantPrograms = allPrograms
                .Where(p =>
                {
                    var name = p.ProgramName?.ToLowerInvariant() ?? "";

                    // STEM programs if strong in math/science
                    if (hasStem && (name.Contains("công nghệ thông tin") || name.Contains("kỹ thuật") 
                        || name.Contains("công nghệ") || name.Contains("khoa học máy tính")))
                        return true;

                    // Humanities/Business if strong in language/social
                    if (hasHumanities && (name.Contains("quản trị") || name.Contains("kinh tế") 
                        || name.Contains("ngôn ngữ") || name.Contains("du lịch")))
                        return true;

                    // Include balanced programs for all students
                    if (name.Contains("quản trị kinh doanh") || name.Contains("marketing"))
                        return true;

                    return false;
                })
                .Take(MaxProgramsForLlm)
                .ToList();

            // Fallback: if filtering is too aggressive, take top 20 by alphabetical
            if (relevantPrograms.Count < 10)
            {
                relevantPrograms = allPrograms.Take(MaxProgramsForLlm).ToList();
            }

            _logger.LogInformation(
                "MajorAdvisorAgent: Filtered to {Count} programs for transcript (strong subjects: {Subjects})",
                relevantPrograms.Count, string.Join(", ", strongSubjects));

            return relevantPrograms;
        }

        // For SchoolRank: filter by rank and combined score
        if (docType == DocumentType.SchoolRank && scores.SchoolRank != null)
        {
            var rank = scores.SchoolRank.Rank ?? int.MaxValue;
            var grade12Score = scores.SchoolRank.Grade12Score ?? 0;

            // Top ranks get access to all programs
            if (rank <= 100)
            {
                _logger.LogInformation(
                    "MajorAdvisorAgent: SchoolRank Top{Rank} - all programs accessible",
                    rank);
                return allPrograms.Take(MaxProgramsForLlm).ToList();
            }

            // Filter by combined score for lower ranks
            var relevantPrograms = allPrograms
                .Where(p =>
                {
                    var name = p.ProgramName?.ToLowerInvariant() ?? "";

                    // High-demand programs need ≥25
                    if ((name.Contains("công nghệ thông tin") || name.Contains("khoa học máy tính") 
                        || name.Contains("y") || name.Contains("dược")) 
                        && grade12Score < 25)
                        return false;

                    // Standard programs accessible at ≥21
                    if (grade12Score < 21)
                        return false;

                    return true;
                })
                .Take(MaxProgramsForLlm)
                .ToList();

            _logger.LogInformation(
                "MajorAdvisorAgent: Filtered to {Count} programs for SchoolRank (Rank: {Rank}, Score: {Score})",
                relevantPrograms.Count, rank, grade12Score);

            return relevantPrograms;
        }

        // Fallback: return top N programs
        return allPrograms.Take(MaxProgramsForLlm).ToList();
    }

    // ── Step 5: Get program recommendations ─────────────────────────────────────

    private async Task<(List<ProgramRecommendation> Recommendations, string RawResponse)> GetRecommendationsAsync(
        DocumentType docType,
        ExtractedScores scores,
        List<Program> programs,
        string fileName,
        CancellationToken cancellationToken)
    {
        var docTypeStr = docType switch
        {
            DocumentType.Transcript => "transcript",
            DocumentType.CompetencyTest => "competency_test",
            DocumentType.SchoolRank => "schoolrank",
            _ => "unknown"
        };
        var scoresJson = JsonSerializer.Serialize(scores, ResponseDeserializerOptions);
        var programsJson = JsonSerializer.Serialize(programs.Select(p => new
        {
            p.ProgramId,
            p.ProgramName,
            p.Description,
            p.Duration,
            p.CareerProspects
        }), ResponseDeserializerOptions);

        var prompt = $"""
            [DOCUMENT_TYPE]: {docTypeStr}

            [SCORES]: 
            {scoresJson}

            [PROGRAMS]:
            {programsJson}

            {MajorAdvisorAgentPrompts.ProgramRecommendation}
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
        var recommendations = ParseOllamaResponse<List<OllamaProgramRecommendationResponse>>(responseBody, fileName);

        // Build subject score dictionary for match calculation
        Dictionary<string, decimal> studentScores;
        decimal? schoolRankScore = null;
        int? schoolRank = null;
        decimal? competencyTotalScore = null;

        if (docType == DocumentType.Transcript && scores.Transcript != null)
        {
            studentScores = ProgramSubjectMatcher.BuildScoreDictionary(scores.Transcript);
        }
        else if (docType == DocumentType.CompetencyTest && scores.Competency != null)
        {
            studentScores = ProgramSubjectMatcher.BuildScoreDictionary(scores.Competency);
            competencyTotalScore = scores.Competency.TotalScore; // Pass ĐGNL total score for bonus
        }
        else if (docType == DocumentType.SchoolRank && scores.SchoolRank != null)
        {
            // SchoolRank doesn't have detailed subject scores, use fallback
            studentScores = new Dictionary<string, decimal>();
            schoolRankScore = scores.SchoolRank.Grade12Score;
            schoolRank = scores.SchoolRank.Rank;
        }
        else
        {
            studentScores = new Dictionary<string, decimal>();
        }

        // Calculate match scores and map to DTOs
        var result = recommendations.Select(r =>
        {
            // Find corresponding program to get full name for classification
            var program = programs.FirstOrDefault(p => p.ProgramId == r.ProgramId);
            var programName = program?.ProgramName ?? r.ProgramName;

            // Calculate backend match score with all available performance metrics
            var calculatedMatchScore = ProgramSubjectMatcher.CalculateMatchScore(
                studentScores,
                programName,
                schoolRankScore,
                schoolRank,
                competencyTotalScore);

            return new ProgramRecommendation
            {
                ProgramId = r.ProgramId,
                ProgramName = r.ProgramName,
                MajorName = r.MajorName,
                Description = r.Description,
                Duration = r.Duration,
                CareerProspects = r.CareerProspects,
                MatchScore = calculatedMatchScore, // Use calculated score
                Reasoning = r.Reasoning,
                Strengths = r.Strengths,
                Concerns = r.Concerns,
                AdmissionMethod = r.AdmissionMethod
            };
        })
        .OrderByDescending(r => r.MatchScore) // Sort by calculated match score
        .ToList();

        return (result, responseBody);
    }

    // ── Step 6: Log to AgentLog ───────────────────────────────────────────────

    private async Task LogToAgentLogAsync(
        MajorAdvisorResult result,
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
                Status = "llm_response",
                OutputData = outputData,
                CreatedAt = DateTime.Now // Use local time for PostgreSQL timestamp without time zone
            };

            await unitOfWork.AgentLogs.AddAsync(log);
            await unitOfWork.SaveChangesAsync();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "MajorAdvisorAgent: Logged to AgentLog (LogId={LogId}, Result={Result}, Duration={Duration}ms)",
                log.LogId, result.Result, duration.TotalMilliseconds);
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

internal sealed class OllamaSchoolRankResponse
{
    public bool Success { get; set; }
    public int? Rank { get; set; }
    public decimal? Grade12Score { get; set; }
    public string? StudentName { get; set; }
    public string? SchoolName { get; set; }
    public int? Year { get; set; }
    public string? ErrorMessage { get; set; }
}

internal sealed class OllamaProgramRecommendationResponse
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string? MajorName { get; set; }
    public string? Description { get; set; }
    public string? Duration { get; set; }
    public string? CareerProspects { get; set; }
    // MatchScore is now calculated in backend, not from LLM
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
    public string AdmissionMethod { get; set; } = string.Empty;
}


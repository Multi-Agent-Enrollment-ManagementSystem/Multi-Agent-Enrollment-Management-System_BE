using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MAEMS.Application.DTOs.MajorAdvisor;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.MultiAgent.RAG.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// Major Advisor Agent - Analyzes academic documents (transcript or competency test)
/// and recommends suitable university programs. Now uses OpenAI GPT-4o-mini for faster vision analysis.
/// </summary>
public sealed class MajorAdvisorAgent : IMajorAdvisorAgent
{
    private readonly IOpenAIService _openAIService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MajorAdvisorAgent> _logger;
    private readonly DocumentIntakeAgentPdfConverter _pdfConverter;
    private readonly IRagVectorStore _vectorStore;
    private readonly IRagEmbeddingService _embeddingService;

    // Static cache for programs (refreshed every 5 minutes)
    private static List<Program>? _cachedPrograms;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly SemaphoreSlim _cacheLock = new(1, 1);
    private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);

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
        IOpenAIService openAIService,
        IServiceScopeFactory scopeFactory,
        ILogger<MajorAdvisorAgent> logger,
        IRagVectorStore vectorStore,
        IRagEmbeddingService embeddingService)
    {
        _openAIService = openAIService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pdfConverter = new DocumentIntakeAgentPdfConverter(logger);
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
    }

    /// <inheritdoc />
    public async Task<MajorAdvisorResult> AnalyzeAndRecommendAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var result = new MajorAdvisorResult
        {
            Result = "failed",
            Status = "llm_response"
        };

        try
        {
            _logger.LogInformation(
                "MajorAdvisorAgent: Starting analysis for file '{FileName}' ({Size} bytes)",
                file.FileName, file.Length);

            // Step 1: Prepare images
            var images = await PrepareImagesAsync(file, cancellationToken);

            // Step 2: Detect document type AND extract scores in one call (performance optimization)
            var (docType, scores, _) = await DetectAndExtractAsync(images, file.FileName, cancellationToken);

            if (docType == DocumentType.Unknown)
            {
                result.Summary = "Không thể xác định loại tài liệu. Vui lòng tải lên học bạ THPT, kết quả thi ĐGNL, hoặc chứng nhận SchoolRank.";
                return result;
            }

            // Set detected document type and scores
            result.DetectedDocumentType = docType switch
            {
                DocumentType.Transcript => "transcript",
                DocumentType.CompetencyTest => "competency_test",
                DocumentType.SchoolRank => "schoolrank",
                _ => "unknown"
            };
            result.Scores = scores;

            // Step 3: Search relevant programs using Qdrant vector search
            var relevantPrograms = await SearchRelevantProgramsAsync(docType, scores, cancellationToken);

            if (relevantPrograms.Count == 0)
            {
                result.Summary = "Không tìm thấy chương trình phù hợp. Vui lòng thử lại với tài liệu khác.";
                return result;
            }

            // Step 4: Get program recommendations
            var (recommendations, _) = await GetRecommendationsAsync(
                docType,
                scores,
                relevantPrograms,
                file.FileName,
                cancellationToken);

            result.Recommendations = recommendations;
            result.Result = "passed";

            // Build summary
            var topProgram = recommendations.FirstOrDefault();
            var docTypeStr = docType switch
            {
                DocumentType.Transcript => "Học bạ",
                DocumentType.CompetencyTest => "ĐGNL",
                DocumentType.SchoolRank => "SchoolRank",
                _ => "Unknown"
            };

            var summaryParts = new List<string>
            {
                $"Phân tích {docTypeStr} thành công.",
                $"Tìm thấy {recommendations.Count} chương trình phù hợp."
            };

            if (topProgram != null)
            {
                summaryParts.Add($"Gợi ý hàng đầu: {topProgram.ProgramName} (match: {topProgram.MatchScore}/100).");
            }

            result.Summary = string.Join(" ", summaryParts);

            _logger.LogInformation(
                "MajorAdvisorAgent: Analysis completed for '{FileName}' - {Count} recommendations generated",
                file.FileName, recommendations.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MajorAdvisorAgent: Error analyzing '{FileName}'", file.FileName);

            result.Result = "failed";
            result.Summary = $"Lỗi khi phân tích tài liệu: {ex.Message}";

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

    // ── Step 2: Combined document analysis (detect type + extract scores in ONE call) ────

    private async Task<(DocumentType Type, ExtractedScores Scores, string RawResponse)> DetectAndExtractAsync(
        List<string> images,
        string fileName,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("MajorAdvisorAgent: Calling OpenAI Vision API for '{FileName}'", fileName);

        // Call OpenAI Vision API with reduced token limit for extraction (JSON only)
        var responseBody = await _openAIService.GetVisionCompletionAsync(
            systemPrompt: "You are an expert at analyzing Vietnamese academic documents. Extract data accurately in JSON format.",
            userMessage: MajorAdvisorAgentPrompts.CombinedDocumentAnalysis,
            base64Images: images,
            maxTokens: 1200,
            cancellationToken: cancellationToken);

        // Debug: Log raw response
        _logger.LogInformation(
            "MajorAdvisorAgent: Raw OpenAI response (first 1000 chars): {Response}",
            responseBody.Length > 1000 ? responseBody[..1000] : responseBody);

        // Parse JSON from response (OpenAI might wrap it in markdown)
        var jsonContent = ExtractJsonFromResponse(responseBody);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaCombinedAnalysisResponse>(
            jsonContent,
            ResponseDeserializerOptions)
            ?? throw new InvalidOperationException($"Failed to parse OpenAI response for '{fileName}'");

        // Parse document type
        var docType = ollamaResponse.DocumentType?.ToLowerInvariant() switch
        {
            "transcript" => DocumentType.Transcript,
            "competency_test" => DocumentType.CompetencyTest,
            "schoolrank" => DocumentType.SchoolRank,
            _ => DocumentType.Unknown
        };

        _logger.LogInformation(
            "MajorAdvisorAgent: Document analyzed as '{Type}' (confidence: {Confidence:F2})",
            docType, ollamaResponse.Confidence);

        // Debug: Log extracted data availability
        _logger.LogInformation(
            "MajorAdvisorAgent: ExtractedData availability - Transcript: {HasTranscript}, Competency: {HasCompetency}, SchoolRank: {HasSchoolRank}",
            ollamaResponse.ExtractedData?.Transcript != null,
            ollamaResponse.ExtractedData?.Competency != null,
            ollamaResponse.ExtractedData?.SchoolRank != null);

        // Parse extracted data based on document type
        var scores = new ExtractedScores();

        if (docType == DocumentType.Transcript && ollamaResponse.ExtractedData?.Transcript != null)
        {
            var transcript = ollamaResponse.ExtractedData.Transcript;
            scores.Transcript = new TranscriptData
            {
                Grade11_Toan = transcript.Grade11?.Toan,
                Grade11_NguVan = transcript.Grade11?.NguVan,
                Grade11_NgoaiNgu = transcript.Grade11?.NgoaiNgu,
                Grade11_VatLy = transcript.Grade11?.VatLy,
                Grade11_HoaHoc = transcript.Grade11?.HoaHoc,
                Grade11_SinhHoc = transcript.Grade11?.SinhHoc,
                Grade11_LichSu = transcript.Grade11?.LichSu,
                Grade11_DiaLy = transcript.Grade11?.DiaLy,
                Grade11_GDCD = transcript.Grade11?.GDCD,
                Grade12_Toan = transcript.Grade12?.Toan,
                Grade12_NguVan = transcript.Grade12?.NguVan,
                Grade12_NgoaiNgu = transcript.Grade12?.NgoaiNgu,
                Grade12_VatLy = transcript.Grade12?.VatLy,
                Grade12_HoaHoc = transcript.Grade12?.HoaHoc,
                Grade12_SinhHoc = transcript.Grade12?.SinhHoc,
                Grade12_LichSu = transcript.Grade12?.LichSu,
                Grade12_DiaLy = transcript.Grade12?.DiaLy,
                Grade12_GDCD = transcript.Grade12?.GDCD
            };
        }
        else if (docType == DocumentType.CompetencyTest && ollamaResponse.ExtractedData?.Competency != null)
        {
            var competency = ollamaResponse.ExtractedData.Competency;

            _logger.LogInformation(
                "MajorAdvisorAgent: Competency data - Success: {Success}, TotalScore: {TotalScore}, TiengViet: {TiengViet}, TiengAnh: {TiengAnh}, ToanHoc: {ToanHoc}, TuDuyKhoaHoc: {TuDuyKhoaHoc}",
                competency.Success,
                competency.TotalScore,
                competency.TiengViet,
                competency.TiengAnh,
                competency.ToanHoc,
                competency.TuDuyKhoaHoc);

            scores.Competency = new CompetencyData
            {
                TotalScore = competency.TotalScore,
                TiengViet = competency.TiengViet,
                TiengAnh = competency.TiengAnh,
                ToanHoc = competency.ToanHoc,
                TuDuyKhoaHoc = competency.TuDuyKhoaHoc,
                PercentileRange = competency.PercentileRange
            };
        }
        else if (docType == DocumentType.SchoolRank && ollamaResponse.ExtractedData?.SchoolRank != null)
        {
            var schoolrank = ollamaResponse.ExtractedData.SchoolRank;
            scores.SchoolRank = new SchoolRankData
            {
                Rank = schoolrank.Rank,
                Grade12Score = schoolrank.Grade12Score,
                StudentName = schoolrank.StudentName,
                SchoolName = schoolrank.SchoolName,
                Year = schoolrank.Year
            };
        }

        return (docType, scores, responseBody);
    }





    // ══════════════════════════════════════════════════════════════════════════════
    // DEPRECATED METHODS - Replaced by Qdrant vector search (SearchRelevantProgramsAsync)
    // Kept for reference and potential fallback scenarios
    // ══════════════════════════════════════════════════════════════════════════════



    // ── Helper: Build semantic query from student scores ─────────────────────────

    private string BuildSemanticQueryFromScores(DocumentType docType, ExtractedScores scores)
    {
        var queryParts = new List<string>();

        if (docType == DocumentType.Transcript && scores.Transcript != null)
        {
            var transcript = scores.Transcript;
            var strongSubjects = new List<string>();
            var avgSubjects = new List<string>();

            // Identify strong subjects (≥8.0) and average subjects (7.0-7.9)
            void CheckSubject(decimal? score, string name)
            {
                if (score >= 8.0m) strongSubjects.Add(name);
                else if (score >= 7.0m) avgSubjects.Add(name);
            }

            CheckSubject(transcript.Grade12_Toan, "Toán học");
            CheckSubject(transcript.Grade12_VatLy, "Vật lý");
            CheckSubject(transcript.Grade12_HoaHoc, "Hóa học");
            CheckSubject(transcript.Grade12_SinhHoc, "Sinh học");
            CheckSubject(transcript.Grade12_NguVan, "Ngữ văn");
            CheckSubject(transcript.Grade12_LichSu, "Lịch sử");
            CheckSubject(transcript.Grade12_DiaLy, "Địa lý");
            CheckSubject(transcript.Grade12_NgoaiNgu, "Tiếng Anh");

            queryParts.Add("Học sinh xuất sắc");

            if (strongSubjects.Count > 0)
            {
                queryParts.Add($"giỏi các môn {string.Join(", ", strongSubjects)}");
            }

            if (avgSubjects.Count > 0)
            {
                queryParts.Add($"khá các môn {string.Join(", ", avgSubjects)}");
            }

            if (transcript.AverageGpa.HasValue)
            {
                queryParts.Add($"GPA trung bình {transcript.AverageGpa:F2}");
            }

            // Infer interest domains
            var hasStem = strongSubjects.Any(s => s.Contains("Toán") || s.Contains("Vật lý") || s.Contains("Hóa"));
            var hasBio = strongSubjects.Any(s => s.Contains("Sinh"));
            var hasHumanities = strongSubjects.Any(s => s.Contains("Ngữ văn") || s.Contains("Lịch sử") || s.Contains("Địa lý"));

            if (hasStem && hasBio)
            {
                queryParts.Add("quan tâm ngành khoa học tự nhiên, công nghệ, y dược");
            }
            else if (hasStem)
            {
                queryParts.Add("quan tâm ngành công nghệ thông tin, kỹ thuật, khoa học máy tính");
            }
            else if (hasHumanities)
            {
                queryParts.Add("quan tâm ngành kinh tế, quản trị kinh doanh, ngôn ngữ, du lịch");
            }
        }
        else if (docType == DocumentType.CompetencyTest && scores.Competency != null)
        {
            var competency = scores.Competency;

            queryParts.Add($"Kết quả thi ĐGNL {competency.TotalScore}/1200");

            if (competency.ToanHoc.HasValue && competency.ToanHoc >= 200)
            {
                queryParts.Add($"Toán học {competency.ToanHoc}/300 (giỏi)");
            }

            if (competency.TuDuyKhoaHoc.HasValue && competency.TuDuyKhoaHoc >= 200)
            {
                queryParts.Add($"Tư duy khoa học {competency.TuDuyKhoaHoc}/300 (giỏi)");
            }

            if (competency.TiengViet.HasValue && competency.TiengViet >= 200)
            {
                queryParts.Add($"Tiếng Việt {competency.TiengViet}/300 (giỏi)");
            }

            if (competency.TiengAnh.HasValue && competency.TiengAnh >= 200)
            {
                queryParts.Add($"Tiếng Anh {competency.TiengAnh}/300 (giỏi)");
            }

            // Infer interest based on highest score
            var scores_ = new List<(string Subject, decimal? Score)>
            {
                ("Toán học và Tư duy khoa học", (competency.ToanHoc ?? 0) + (competency.TuDuyKhoaHoc ?? 0)),
                ("Tiếng Việt và Tiếng Anh", (competency.TiengViet ?? 0) + (competency.TiengAnh ?? 0))
            };

            var strongest = scores_.OrderByDescending(s => s.Score).First();

            if (strongest.Subject.Contains("Toán"))
            {
                queryParts.Add("quan tâm ngành công nghệ, kỹ thuật, khoa học máy tính");
            }
            else
            {
                queryParts.Add("quan tâm ngành kinh tế, quản trị, ngôn ngữ, truyền thông");
            }
        }
        else if (docType == DocumentType.SchoolRank && scores.SchoolRank != null)
        {
            var schoolrank = scores.SchoolRank;

            queryParts.Add($"Học sinh đạt SchoolRank Top{schoolrank.Rank}");

            if (schoolrank.Grade12Score.HasValue)
            {
                queryParts.Add($"Điểm HK1 lớp 12: {schoolrank.Grade12Score}/30");
            }

            if (schoolrank.Rank <= 50)
            {
                queryParts.Add("Học sinh xuất sắc toàn diện, phù hợp các ngành đào tạo chất lượng cao");
            }
        }

        var query = string.Join(". ", queryParts);

        _logger.LogInformation(
            "MajorAdvisorAgent: Built semantic query from {DocType}: '{Query}'",
            docType, query);

        return query;
    }

    // ── Helper: Search relevant programs using Qdrant vector search ──────────────

    private async Task<List<Program>> SearchRelevantProgramsAsync(
        DocumentType docType,
        ExtractedScores scores,
        CancellationToken cancellationToken)
    {
        const int TopK = 60; // Increased from 20 to provide more diverse program choices

        try
        {
            // Step 1: Build semantic query from student scores
            var semanticQuery = BuildSemanticQueryFromScores(docType, scores);

            // Step 2: Generate embedding for the query
            var queryEmbedding = await _embeddingService.EmbedTextAsync(semanticQuery, cancellationToken);

            _logger.LogInformation(
                "MajorAdvisorAgent: Generated query embedding (dimension: {Dimension})",
                queryEmbedding.Length);

            // Step 3: Search Qdrant for similar programs
            var searchResults = await _vectorStore.SearchAsync(queryEmbedding, TopK, cancellationToken);
            var resultList = searchResults.ToList();

            _logger.LogInformation(
                "MajorAdvisorAgent: Qdrant returned {Count} similar programs",
                resultList.Count);

            // Step 4: Filter to only program-type documents and extract metadata
            var programIds = resultList
                .Where(r => r.Document.Metadata.ContainsKey("type") && r.Document.Metadata["type"] == "program")
                .Select(r =>
                {
                    if (r.Document.Metadata.TryGetValue("program_id", out var idStr) && int.TryParse(idStr, out var id))
                    {
                        return (int?)id;
                    }
                    return null;
                })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (programIds.Count == 0)
            {
                _logger.LogWarning("MajorAdvisorAgent: No valid program IDs found in Qdrant results, falling back to database");
                return await LoadProgramsFromDbFallbackAsync(cancellationToken);
            }

            // Step 5: Load full Program entities from database by IDs
            await using var scope = _scopeFactory.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var programs = await unitOfWork.Programs.GetAllAsync();
            var relevantPrograms = programs
                .Where(p => programIds.Contains(p.ProgramId) && p.IsActive == true)
                .ToList();

            _logger.LogInformation(
                "MajorAdvisorAgent: Loaded {Count} relevant programs from database via Qdrant search",
                relevantPrograms.Count);

            return relevantPrograms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "MajorAdvisorAgent: Error during Qdrant search, falling back to database");

            // Fallback to database-based approach
            return await LoadProgramsFromDbFallbackAsync(cancellationToken);
        }
    }

    // ── Fallback: Load programs from database when Qdrant fails ──────────────────

    private async Task<List<Program>> LoadProgramsFromDbFallbackAsync(CancellationToken cancellationToken)
    {
        _logger.LogWarning("MajorAdvisorAgent: Using database fallback for program loading");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var programs = await unitOfWork.Programs.GetAllAsync();
        var activePrograms = programs.Where(p => p.IsActive == true).ToList(); // Load ALL active programs for maximum diversity

        _logger.LogInformation(
            "MajorAdvisorAgent: Loaded {Count} active programs from database (fallback)",
            activePrograms.Count);

        return activePrograms;
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
        // Send only essential fields to LLM to reduce token count (details enriched later from DB)
        var programsJson = JsonSerializer.Serialize(programs.Select(p => new
        {
            p.ProgramId,
            p.ProgramName
        }), ResponseDeserializerOptions);

        var prompt = $"""
            [DOCUMENT_TYPE]: {docTypeStr}

            [SCORES]: 
            {scoresJson}

            [PROGRAMS]:
            {programsJson}

            {MajorAdvisorAgentPrompts.ProgramRecommendation}
            """;

        _logger.LogInformation("MajorAdvisorAgent: Calling OpenAI API for program recommendations");

        // Call OpenAI text completion with reduced token limit (JSON recommendations only)
        var responseBody = await _openAIService.GetChatCompletionAsync(
            systemPrompt: "You are an expert academic advisor. Analyze student scores and recommend suitable university programs in JSON format.",
            userMessage: prompt,
            conversationHistory: null,
            maxTokens: 1800,
            cancellationToken: cancellationToken);

        // Parse JSON from response
        var jsonContent = ExtractJsonFromResponse(responseBody);
        var recommendations = JsonSerializer.Deserialize<List<OllamaProgramRecommendationResponse>>(
            jsonContent,
            ResponseDeserializerOptions)
            ?? throw new InvalidOperationException($"Failed to parse OpenAI recommendation response for '{fileName}'");

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

        // Calculate match scores and map to simplified DTOs
        var result = recommendations.Select(r =>
        {
            // Find corresponding program for name enrichment
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
                ProgramName = programName,
                MatchScore = calculatedMatchScore,
                Reasoning = r.Reasoning,
                Strengths = r.Strengths,
                Concerns = r.Concerns
            };
        })
        .OrderByDescending(r => r.MatchScore) // Sort by calculated match score
        .ToList();

        return (result, responseBody);
    }

    // ── Helper: Extract JSON from OpenAI response (handles markdown wrapper) ────

    private string ExtractJsonFromResponse(string response)
    {
        // OpenAI might wrap JSON in ```json ... ```
        var trimmed = response.Trim();

        if (trimmed.StartsWith("```json"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }
        else if (trimmed.StartsWith("```"))
        {
            var startIndex = trimmed.IndexOf('\n') + 1;
            var endIndex = trimmed.LastIndexOf("```");
            if (endIndex > startIndex)
            {
                return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
            }
        }

        // No wrapper, return as-is
        return trimmed;
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
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("grade_11")]
    public TranscriptGrades? Grade11 { get; set; }

    [JsonPropertyName("grade_12")]
    public TranscriptGrades? Grade12 { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal sealed class TranscriptGrades
{
    [JsonPropertyName("toan")]
    public decimal? Toan { get; set; }

    [JsonPropertyName("ngu_van")]
    public decimal? NguVan { get; set; }

    [JsonPropertyName("ngoai_ngu")]
    public decimal? NgoaiNgu { get; set; }

    [JsonPropertyName("vat_ly")]
    public decimal? VatLy { get; set; }

    [JsonPropertyName("hoa_hoc")]
    public decimal? HoaHoc { get; set; }

    [JsonPropertyName("sinh_hoc")]
    public decimal? SinhHoc { get; set; }

    [JsonPropertyName("lich_su")]
    public decimal? LichSu { get; set; }

    [JsonPropertyName("dia_ly")]
    public decimal? DiaLy { get; set; }

    [JsonPropertyName("gdcd")]
    public decimal? GDCD { get; set; }
}

internal sealed class OllamaCompetencyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("total_score")]
    public decimal? TotalScore { get; set; }

    [JsonPropertyName("tieng_viet")]
    public decimal? TiengViet { get; set; }

    [JsonPropertyName("tieng_anh")]
    public decimal? TiengAnh { get; set; }

    [JsonPropertyName("toan_hoc")]
    public decimal? ToanHoc { get; set; }

    [JsonPropertyName("tu_duy_khoa_hoc")]
    public decimal? TuDuyKhoaHoc { get; set; }

    [JsonPropertyName("percentile_range")]
    public string? PercentileRange { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal sealed class OllamaSchoolRankResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("rank")]
    public int? Rank { get; set; }

    [JsonPropertyName("grade_12_score")]
    public decimal? Grade12Score { get; set; }

    [JsonPropertyName("student_name")]
    public string? StudentName { get; set; }

    [JsonPropertyName("school_name")]
    public string? SchoolName { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

internal sealed class OllamaProgramRecommendationResponse
{
    public int ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    // MatchScore is calculated in backend, not from LLM
    public string Reasoning { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Concerns { get; set; } = new();
}

// ── Combined Analysis Response Model (NEW) ────────────────────────────────────

internal sealed class OllamaCombinedAnalysisResponse
{
    [JsonPropertyName("document_type")]
    public string? DocumentType { get; set; }

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("extracted_data")]
    public ExtractedDataWrapper? ExtractedData { get; set; }
}

internal sealed class ExtractedDataWrapper
{
    [JsonPropertyName("transcript")]
    public OllamaTranscriptResponse? Transcript { get; set; }

    [JsonPropertyName("competency")]
    public OllamaCompetencyResponse? Competency { get; set; }

    [JsonPropertyName("schoolrank")]
    public OllamaSchoolRankResponse? SchoolRank { get; set; }
}


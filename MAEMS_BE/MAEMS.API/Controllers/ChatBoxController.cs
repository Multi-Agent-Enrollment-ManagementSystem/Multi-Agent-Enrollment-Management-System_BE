using MAEMS.Application.DTOs.Chat;
using MAEMS.Application.Features.Chat.Commands.AskChatBox;
using MAEMS.Application.Features.Chat.Queries.GetChatHistory;
using MAEMS.Application.Interfaces;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Repositories;
using MAEMS.MultiAgent.RAG.Interfaces;
using MAEMS.MultiAgent.RAG.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MAEMS.API.Controllers;

/// <summary>
/// Controller để xử lý chatbox Q&A cho thí sinh
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatBoxController : ControllerBase
{
    private readonly IChatBoxAgent _chatBoxAgent;
    private readonly ILlmChatLogRepositoryLegacy _chatLogRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ChatBoxController> _logger;
    private readonly IRagRetrievalService _ragRetrievalService;
    private readonly IRagVectorStore _ragVectorStore;
    private readonly IUnitOfWork _unitOfWork;

    public ChatBoxController(
        IChatBoxAgent chatBoxAgent,
        ILlmChatLogRepositoryLegacy chatLogRepository,
        IMediator mediator,
        ILogger<ChatBoxController> logger,
        IRagRetrievalService ragRetrievalService,
        IRagVectorStore ragVectorStore,
        IUnitOfWork unitOfWork)
    {
        _chatBoxAgent = chatBoxAgent;
        _chatLogRepository = chatLogRepository;
        _mediator = mediator;
        _logger = logger;
        _ragRetrievalService = ragRetrievalService;
        _ragVectorStore = ragVectorStore;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Gửi câu hỏi tới chatbox AI
    /// </summary>
    /// <param name="request">Request chứa câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trả lời từ AI</returns>
    [HttpPost("ask")]
    [Produces("application/json")]
    public async Task<ActionResult<ChatBoxResponseDto>> AskQuestion(
        [FromBody] AskChatBoxRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID in claims");
                return Unauthorized("User ID not found in token");
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return BadRequest(new { message = "Question cannot be empty" });
            }

            if (request.Question.Length > 5000)
            {
                return BadRequest(new { message = "Question is too long (max 5000 characters)" });
            }

            _logger.LogInformation("User {UserId} asked question: {Question}", userId, request.Question);

            // Call ChatBoxAgent to get response
            var response = await _chatBoxAgent.RespondAsync(userId, request.Question, cancellationToken);

            // Get the saved chat log
            var chatLogs = await _chatLogRepository.GetByUserIdAsync(userId, 1, 1, cancellationToken);
            var lastChat = chatLogs.FirstOrDefault();

            return Ok(new ChatBoxResponseDto
            {
                ChatId = lastChat?.ChatId ?? 0,
                Question = request.Question,
                Answer = response,
                CreatedAt = DateTime.Now
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Request was cancelled");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Request timeout. Please try again." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API error - Check Gemini API status");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "AI service is temporarily unavailable. Check your API key and network connection." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid configuration - {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "AI service configuration error. Please contact administrator." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat question - {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while processing your question" });
        }
    }

    /// <summary>
    /// Lấy lịch sử chat của user
    /// </summary>
    /// <param name="pageNumber">Số trang (default 1)</param>
    /// <param name="pageSize">Số messages mỗi trang (default 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Danh sách chat history</returns>
    [HttpGet("history")]
    [Produces("application/json")]
    public async Task<ActionResult<ChatHistoryResponseDto>> GetChatHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Invalid user ID in claims");
                return Unauthorized("User ID not found in token");
            }

            // Validate pagination parameters
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            _logger.LogInformation("Retrieving chat history for user {UserId}, page {PageNumber}, size {PageSize}",
                userId, pageNumber, pageSize);

            // Get total count
            var totalCount = await _chatLogRepository.GetCountByUserIdAsync(userId, cancellationToken);

            // Get paginated messages
            var chatLogs = await _chatLogRepository.GetByUserIdAsync(
                userId,
                pageNumber,
                pageSize,
                cancellationToken);

            // Map to DTO
            var messages = chatLogs.Select(log => new ChatMessageDto
            {
                ChatId = log.ChatId,
                Question = log.UserQuery ?? "",
                Answer = log.LlmResponse ?? "",
                CreatedAt = log.CreatedAt ?? DateTime.UtcNow
            }).ToList();

            // Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return Ok(new ChatHistoryResponseDto
            {
                Messages = messages,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                TotalPages = totalPages,
                PageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving chat history" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> Health()
    {
        return Ok(new { status = "ChatBox service is running" });
    }

    /// <summary>
    /// Debug endpoint to check RAG system status and collection info
    /// </summary>
    //[HttpGet("debug/rag-status")]
    //[AllowAnonymous]
    //public async Task<IActionResult> CheckRagStatus(CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Checking RAG system status");

    //        var status = new
    //        {
    //            timestamp = DateTime.UtcNow,
    //            serviceName = "ChatBox RAG",
    //            components = new
    //            {
    //                qdrant = new
    //                {
    //                    url = "http://localhost:6333",
    //                    status = "Checking..."
    //                },
    //                embedding = new
    //                {
    //                    service = "Gemini",
    //                    status = "Ready"
    //                }
    //            },
    //            collection = new
    //            {
    //                name = "admission_documents",
    //                status = "Check with /api/chatbox/debug/collection-info"
    //            },
    //            instructions = new
    //            {
    //                step1 = "Make sure Qdrant is running: docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant:latest",
    //                step2 = "Call POST /api/chatbox/test-index-documents to create collection and index documents",
    //                step3 = "Then use POST /api/chatbox/ask with JWT token to ask questions"
    //            }
    //        };

    //        return Ok(status);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error checking RAG status");
    //        return StatusCode(StatusCodes.Status500InternalServerError,
    //            new { error = "Failed to check status", message = ex.Message });
    //    }
    //}

    /// <summary>
    /// Check if collection exists in Qdrant
    /// </summary>
    //[HttpGet("debug/collection-info")]
    //[AllowAnonymous]
    //public async Task<IActionResult> GetCollectionInfo(CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Retrieving collection information");

    //        // Get document count from vector store
    //        var docCount = await _ragVectorStore.GetDocumentCountAsync(cancellationToken);

    //        return Ok(new
    //        {
    //            collectionName = "admission_documents",
    //            documentCount = docCount,
    //            status = docCount > 0 ? "Active" : "Empty or not created yet",
    //            message = docCount == 0 
    //                ? "Collection is empty. Call POST /api/chatbox/test-index-documents to create and index documents"
    //                : $"Collection contains {docCount} documents",
    //            qdrantUrl = "http://localhost:6333/dashboard"
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error getting collection info");
    //        return Ok(new
    //        {
    //            collectionName = "admission_documents",
    //            status = "Not created or connection error",
    //            error = ex.Message,
    //            nextStep = "Call POST /api/chatbox/test-index-documents"
    //        });
    //    }
    //}

    /// <summary>
    /// Get ALL documents from collection for debugging
    /// </summary>
    //[HttpGet("debug/all-documents")]
    //[AllowAnonymous]
    //public async Task<IActionResult> GetAllDocuments(CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Retrieving all documents from collection");

    //        // Get total count
    //        var totalCount = await _ragVectorStore.GetDocumentCountAsync(cancellationToken);

    //        if (totalCount == 0)
    //        {
    //            return Ok(new
    //            {
    //                collectionName = "admission_documents",
    //                totalDocuments = 0,
    //                status = "Empty",
    //                message = "Collection is empty. Call POST /api/chatbox/index-from-database to load documents",
    //                documents = new List<object>()
    //            });
    //        }

    //        // Get all documents by searching with a neutral embedding
    //        var neutralEmbedding = new float[3072];

    //        // Search with high topK to get all documents
    //        var allDocs = await _ragVectorStore.SearchAsync(neutralEmbedding, Math.Min(totalCount, 1000), cancellationToken);

    //        var documents = allDocs.Select(doc => new
    //        {
    //            id = doc.Document.Id,
    //            content = doc.Document.Content,
    //            source = doc.Document.Source,
    //            metadata = doc.Document.Metadata,
    //            createdAt = doc.Document.CreatedAt,
    //            similarityScore = Math.Round(doc.Score, 4)
    //        }).ToList();

    //        return Ok(new
    //        {
    //            collectionName = "admission_documents",
    //            totalDocuments = totalCount,
    //            returnedDocuments = documents.Count,
    //            status = "Active",
    //            documents = documents,
    //            summary = new
    //            {
    //                programs = documents.Count(d => (d.metadata as Dictionary<string, string>)?["type"] == "program"),
    //                admissionTypes = documents.Count(d => (d.metadata as Dictionary<string, string>)?["type"] == "admission_type"),
    //                admissionConfigs = documents.Count(d => (d.metadata as Dictionary<string, string>)?["type"] == "admission_config")
    //            }
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving all documents");
    //        return StatusCode(StatusCodes.Status500InternalServerError,
    //            new { error = "Failed to retrieve documents", message = ex.Message });
    //    }
    //}

    /// <summary>
    /// Get sample documents from collection for debugging
    /// </summary>
    //[HttpGet("debug/sample-documents")]
    //[AllowAnonymous]
    //public async Task<IActionResult> GetSampleDocuments([FromQuery] int limit = 5, CancellationToken cancellationToken = default)
    //{
    //    try
    //    {
    //        _logger.LogInformation("Retrieving sample documents from collection");

    //        if (limit < 1 || limit > 20)
    //            limit = 5;

    //        // Create a test query embedding (you would normally use a real question)
    //        // For now, we'll create a dummy embedding
    //        var dummyEmbedding = new float[3072];
    //        for (int i = 0; i < 100; i++)
    //        {
    //            dummyEmbedding[i] = 0.5f;
    //        }

    //        // Search for similar documents (which will return the top matches)
    //        var similarDocs = await _ragVectorStore.SearchAsync(dummyEmbedding, limit, cancellationToken);

    //        var documents = similarDocs.Select(doc => new
    //        {
    //            id = doc.Document.Id,
    //            content = doc.Document.Content.Length > 200 
    //                ? doc.Document.Content.Substring(0, 200) + "..." 
    //                : doc.Document.Content,
    //            source = doc.Document.Source,
    //            similarity = doc.Score,
    //            metadata = doc.Document.Metadata,
    //            createdAt = doc.Document.CreatedAt
    //        }).ToList();

    //        return Ok(new
    //        {
    //            collectionName = "admission_documents",
    //            sampleSize = documents.Count,
    //            documents = documents,
    //            note = "Returned top matching documents from collection"
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error retrieving sample documents");
    //        return StatusCode(StatusCodes.Status500InternalServerError,
    //            new { error = "Failed to retrieve documents", message = ex.Message });
    //    }
    //}

    /// <summary>
    /// Search documents by query (for testing RAG)
    /// </summary>
    //[HttpPost("debug/search-documents")]
    //[AllowAnonymous]
    //public async Task<IActionResult> SearchDocuments([FromBody] SearchRequest request, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        if (string.IsNullOrWhiteSpace(request.Query))
    //            return BadRequest(new { error = "Query cannot be empty" });

    //        _logger.LogInformation("Searching documents with query: {Query}", request.Query);

    //        // Retrieve documents using the retrieval service
    //        var topK = request.TopK ?? 5;
    //        if (topK < 1) topK = 5;
    //        if (topK > 20) topK = 20;

    //        var results = await _ragRetrievalService.RetrieveAsync(request.Query, topK, cancellationToken);

    //        var documents = results.Select(doc => new
    //        {
    //            id = doc.Id,
    //            content = doc.Content.Length > 300 
    //                ? doc.Content.Substring(0, 300) + "..." 
    //                : doc.Content,
    //            source = doc.Source,
    //            metadata = doc.Metadata,
    //            createdAt = doc.CreatedAt
    //        }).ToList();

    //        return Ok(new
    //        {
    //            query = request.Query,
    //            resultCount = documents.Count,
    //            results = documents
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error searching documents");
    //        return StatusCode(StatusCodes.Status500InternalServerError,
    //            new { error = "Search failed", message = ex.Message });
    //    }
    //}

    /// <summary>
    /// Test endpoint to index sample documents for RAG (Development only)
    /// </summary>
    [HttpPost("test-index-documents")]
    [AllowAnonymous]
    public async Task<IActionResult> TestIndexDocuments(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting test document indexing");

            // Create sample documents for testing
            var sampleDocuments = new List<RagDocument>
            {
                new RagDocument
                {
                    Id = "doc-001",
                    Content = "Admission requirements: Applicants must have a bachelor's degree in a related field. " +
                             "Minimum GPA of 3.0 is required. TOEFL score of at least 80 is needed for international students.",
                    Source = "AdmissionGuide.pdf",
                    Metadata = new Dictionary<string, string> { { "section", "requirements" } },
                    CreatedAt = DateTime.UtcNow
                },
                new RagDocument
                {
                    Id = "doc-002",
                    Content = "Application deadline: The application deadline for Fall 2024 is March 31, 2024. " +
                             "Late applications may be considered on a rolling basis if seats are available.",
                    Source = "ImportantDates.pdf",
                    Metadata = new Dictionary<string, string> { { "section", "deadline" } },
                    CreatedAt = DateTime.UtcNow
                },
                new RagDocument
                {
                    Id = "doc-003",
                    Content = "Tuition and fees: Annual tuition is $45,000. Additional fees include $2,000 for health insurance, " +
                             "$1,500 for technology fee, and $500 for student activity fee.",
                    Source = "FinancialInformation.pdf",
                    Metadata = new Dictionary<string, string> { { "section", "fees" } },
                    CreatedAt = DateTime.UtcNow
                },
                new RagDocument
                {
                    Id = "doc-004",
                    Content = "Scholarship opportunities: Merit-based scholarships up to $20,000 are available. " +
                             "Need-based aid is also offered to qualified applicants. FAFSA completion is required.",
                    Source = "Scholarships.pdf",
                    Metadata = new Dictionary<string, string> { { "section", "financial_aid" } },
                    CreatedAt = DateTime.UtcNow
                },
                new RagDocument
                {
                    Id = "doc-005",
                    Content = "Program overview: This master's program focuses on advanced topics in the field. " +
                             "Students will complete 36 credit hours of coursework including core classes, electives, and a capstone project.",
                    Source = "ProgramDescription.pdf",
                    Metadata = new Dictionary<string, string> { { "section", "program" } },
                    CreatedAt = DateTime.UtcNow
                }
            };

            // Index the documents
            await _ragRetrievalService.IndexDocumentsAsync(sampleDocuments, cancellationToken);

            _logger.LogInformation("Test document indexing completed successfully");
            return Ok(new
            {
                message = "Test documents indexed successfully",
                count = sampleDocuments.Count,
                documents = sampleDocuments.Select(d => new { d.Id, d.Source })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during test indexing");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Test indexing failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Debug: Check what data exists in database before indexing
    /// </summary>
    [HttpGet("debug/database-info")]
    [AllowAnonymous]
    public async Task<IActionResult> CheckDatabaseInfo(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Checking database for indexable data");

            var programsDb = await _unitOfWork.Programs.GetAllAsync();
            var admissionTypesDb = await _unitOfWork.AdmissionTypes.GetAllAsync();
            var programConfigsDb = await _unitOfWork.ProgramAdmissionConfigs.GetAllAsync();
            var majorsDb = await _unitOfWork.Majors.GetAllAsync();

            var programsActive = programsDb.Where(p => p.IsActive == true).ToList();
            var typesActive = admissionTypesDb.Where(a => a.IsActive == true).ToList();
            var configsActive = programConfigsDb.Where(c => c.IsActive == true).ToList();
            var majorsActive = majorsDb.Where(m => m.IsActive == true).ToList();

            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                totalRecords = new
                {
                    programs = programsDb.Count(),
                    programsActive = programsActive.Count,
                    admissionTypes = admissionTypesDb.Count(),
                    admissionTypesActive = typesActive.Count,
                    programConfigs = programConfigsDb.Count(),
                    programConfigsActive = configsActive.Count,
                    majors = majorsDb.Count(),
                    majorsActive = majorsActive.Count
                },
                sample = new
                {
                    programs = programsActive.Take(3).Select(p => new 
                    { 
                        p.ProgramId, 
                        p.ProgramName, 
                        p.IsActive, 
                        p.Description 
                    }),
                    admissionTypes = typesActive.Take(3).Select(a => new 
                    { 
                        a.AdmissionTypeId, 
                        a.AdmissionTypeName, 
                        a.IsActive,
                        a.Type
                    }),
                    programConfigs = configsActive.Take(3).Select(c => new 
                    { 
                        c.ConfigId, 
                        c.ProgramName, 
                        c.CampusName,
                        c.IsActive,
                        c.Quota
                    })
                },
                totalEstimatedDocuments = programsActive.Count + typesActive.Count + configsActive.Count + majorsActive.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to check database", message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Load admission data from database and index into RAG
    /// </summary>
    [HttpPost("index-from-database")]
    [AllowAnonymous]
    public async Task<IActionResult> IndexFromDatabase(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting to load admission data from database");

            var ragDocuments = new List<RagDocument>();

            // Load Programs
            var programs = await _unitOfWork.Programs.GetAllAsync();
            var programsActive = programs.Where(p => p.IsActive == true).ToList();

            _logger.LogInformation($"Found {programsActive.Count} active programs");

            foreach (var program in programsActive)
            {
                var content = $"Program: {program.ProgramName}\n";
                if (!string.IsNullOrEmpty(program.Description))
                    content += $"Description: {program.Description}\n";
                if (!string.IsNullOrEmpty(program.CareerProspects))
                    content += $"Career Prospects: {program.CareerProspects}\n";
                if (!string.IsNullOrEmpty(program.Duration))
                    content += $"Duration: {program.Duration}\n";
                if (!string.IsNullOrEmpty(program.EnrollmentYear))
                    content += $"Enrollment Year: {program.EnrollmentYear}\n";

                ragDocuments.Add(new RagDocument
                {
                    Id = $"program-{program.ProgramId}",
                    Content = content,
                    Source = $"Program_{program.ProgramName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "program" },
                        { "program_id", program.ProgramId.ToString() },
                        { "program_name", program.ProgramName }
                    },
                    CreatedAt = program.CreatedAt ?? DateTime.UtcNow
                });
            }

            // Load Majors
            var majors = await _unitOfWork.Majors.GetAllAsync();
            var majorsActive = majors.Where(m => m.IsActive == true).ToList();

            _logger.LogInformation($"Found {majorsActive.Count} active majors");

            foreach (var major in majorsActive)
            {
                var content = $"Major: {major.MajorName}\n";
                content += $"Code: {major.MajorCode}\n";
                if (!string.IsNullOrEmpty(major.Description))
                    content += $"Description: {major.Description}\n";

                ragDocuments.Add(new RagDocument
                {
                    Id = $"major-{major.MajorId}",
                    Content = content,
                    Source = $"Major_{major.MajorName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "major" },
                        { "major_id", major.MajorId.ToString() },
                        { "major_name", major.MajorName },
                        { "major_code", major.MajorCode }
                    },
                    CreatedAt = major.CreatedAt ?? DateTime.UtcNow
                });
            }

            // Load Admission Types
            var admissionTypes = await _unitOfWork.AdmissionTypes.GetAllAsync();
            var typesActive = admissionTypes.Where(a => a.IsActive == true).ToList();

            _logger.LogInformation($"Found {typesActive.Count} active admission types");

            foreach (var admissionType in typesActive)
            {
                var content = $"Admission Type: {admissionType.AdmissionTypeName}\n";
                content += $"Type: {admissionType.Type}\n";
                if (!string.IsNullOrEmpty(admissionType.RequiredDocumentList))
                    content += $"Required Documents: {admissionType.RequiredDocumentList}\n";
                if (!string.IsNullOrEmpty(admissionType.EnrollmentYear))
                    content += $"Enrollment Year: {admissionType.EnrollmentYear}\n";

                ragDocuments.Add(new RagDocument
                {
                    Id = $"admission-type-{admissionType.AdmissionTypeId}",
                    Content = content,
                    Source = $"AdmissionType_{admissionType.AdmissionTypeName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "admission_type" },
                        { "admission_type_id", admissionType.AdmissionTypeId.ToString() },
                        { "admission_type_name", admissionType.AdmissionTypeName }
                    },
                    CreatedAt = admissionType.CreatedAt ?? DateTime.UtcNow
                });
            }

            // Load Program Admission Configs
            var admissionConfigs = await _unitOfWork.ProgramAdmissionConfigs.GetAllAsync();
            var configsActive = admissionConfigs.Where(c => c.IsActive == true).ToList();

            _logger.LogInformation($"Found {configsActive.Count} active program configs");

            foreach (var config in configsActive)
            {
                var content = $"Program: {config.ProgramName}\n";
                content += $"Campus: {config.CampusName}\n";
                content += $"Admission Type: {config.AdmissionTypeName}\n";
                if (config.Quota.HasValue)
                    content += $"Quota: {config.Quota} seats\n";

                ragDocuments.Add(new RagDocument
                {
                    Id = $"config-{config.ConfigId}",
                    Content = content,
                    Source = $"AdmissionConfig_{config.ProgramName}_{config.CampusName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "admission_config" },
                        { "config_id", config.ConfigId.ToString() },
                        { "program_name", config.ProgramName ?? "" },
                        { "campus_name", config.CampusName ?? "" },
                        { "quota", config.Quota?.ToString() ?? "0" }
                    },
                    CreatedAt = config.CreatedAt ?? DateTime.UtcNow
                });
            }

            if (ragDocuments.Count == 0)
            {
                return BadRequest(new 
                { 
                    message = "No active admission data found in database",
                    hint = "Make sure data exists and IsActive = true. Check GET /debug/database-info",
                    totalPrograms = programsActive.Count,
                    totalMajors = majorsActive.Count,
                    totalAdmissionTypes = typesActive.Count,
                    totalConfigs = configsActive.Count
                });
            }

            // Clear old collection first
            try
            {
                await _ragVectorStore.ClearAsync(cancellationToken);
                _logger.LogInformation("Cleared old collection");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear old collection, will continue anyway");
            }

            // Index the documents
            _logger.LogInformation($"Indexing {ragDocuments.Count} documents...");
            await _ragRetrievalService.IndexDocumentsAsync(ragDocuments, cancellationToken);

            _logger.LogInformation("Database indexing completed successfully. Indexed {Count} documents", ragDocuments.Count);
            return Ok(new
            {
                message = "Database documents indexed successfully",
                count = ragDocuments.Count,
                breakdown = new
                {
                    programs = ragDocuments.Count(d => d.Metadata?["type"] == "program"),
                    majors = ragDocuments.Count(d => d.Metadata?["type"] == "major"),
                    admissionTypes = ragDocuments.Count(d => d.Metadata?["type"] == "admission_type"),
                    admissionConfigs = ragDocuments.Count(d => d.Metadata?["type"] == "admission_config")
                },
                documents = ragDocuments.Select(d => new { d.Id, d.Source, Type = d.Metadata?["type"] })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database indexing: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Database indexing failed", message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Progressive indexing - index database documents in small batches with long delays
    /// Useful for free tier API with limited quota
    /// POST /api/chatbox/index-from-database-progressive?batchSize=10&delayBetweenBatchesSeconds=30
    /// </summary>
    [HttpPost("index-from-database-progressive")]
    [AllowAnonymous]
    public async Task<IActionResult> IndexFromDatabaseProgressive(
        [FromQuery] int batchSize = 10,
        [FromQuery] int delayBetweenBatchesSeconds = 30,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Progressive indexing started: batchSize={BatchSize}, delay={DelaySeconds}s",
                batchSize, delayBetweenBatchesSeconds);

            // Load all documents
            var programsDb = await _unitOfWork.Programs.GetAllAsync();
            var programsActive = programsDb.Where(p => p.IsActive == true).ToList();

            var majorsDb = await _unitOfWork.Majors.GetAllAsync();
            var majorsActive = majorsDb.Where(m => m.IsActive == true).ToList();

            var typesDb = await _unitOfWork.AdmissionTypes.GetAllAsync();
            var typesActive = typesDb.Where(t => t.IsActive == true).ToList();

            var configsDb = await _unitOfWork.ProgramAdmissionConfigs.GetAllAsync();
            var configsActive = configsDb.Where(c => c.IsActive == true).ToList();

            var ragDocuments = new List<RagDocument>();

            // Create documents from programs
            foreach (var program in programsActive)
            {
                ragDocuments.Add(new RagDocument
                {
                    Id = $"program-{program.ProgramId}",
                    Content = $"Program: {program.ProgramName}\n\nDescription: {program.Description}",
                    Source = $"Program_{program.ProgramName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "program" },
                        { "program_id", program.ProgramId.ToString() },
                        { "program_name", program.ProgramName }
                    }
                });
            }

            // Create documents from majors
            foreach (var major in majorsActive)
            {
                ragDocuments.Add(new RagDocument
                {
                    Id = $"major-{major.MajorId}",
                    Content = $"Major: {major.MajorName}",
                    Source = $"Major_{major.MajorName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "major" },
                        { "major_id", major.MajorId.ToString() },
                        { "major_name", major.MajorName }
                    }
                });
            }

            // Create documents from admission types
            foreach (var type in typesActive)
            {
                ragDocuments.Add(new RagDocument
                {
                    Id = $"admission_type-{type.AdmissionTypeId}",
                    Content = $"Admission Type: {type.AdmissionTypeName}\n\nType: {type.Type}",
                    Source = $"AdmissionType_{type.AdmissionTypeName}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "admission_type" },
                        { "admission_type_id", type.AdmissionTypeId.ToString() },
                        { "admission_type_name", type.AdmissionTypeName }
                    }
                });
            }

            // Create documents from program admission configs
            foreach (var config in configsActive)
            {
                ragDocuments.Add(new RagDocument
                {
                    Id = $"config-{config.ConfigId}",
                    Content = $"Program: {config.ProgramName}\nCampus: {config.CampusName}\nQuota: {config.Quota}",
                    Source = $"Config_{config.ConfigId}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "admission_config" },
                        { "config_id", config.ConfigId.ToString() },
                        { "program_name", config.ProgramName },
                        { "campus_name", config.CampusName },
                        { "quota", config.Quota.ToString() }
                    }
                });
            }

            if (ragDocuments.Count == 0)
            {
                return BadRequest(new
                {
                    message = "No active admission data found in database",
                    hint = "Make sure data exists and IsActive = true"
                });
            }

            // Clear old collection first
            try
            {
                await _ragVectorStore.ClearAsync(cancellationToken);
                _logger.LogInformation("Cleared old collection");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear old collection, will continue anyway");
            }

            // Index in progressive batches
            var totalBatches = (int)Math.Ceiling((double)ragDocuments.Count / batchSize);
            var indexedCount = 0;

            for (int i = 0; i < ragDocuments.Count; i += batchSize)
            {
                var batchNum = i / batchSize + 1;
                var batch = ragDocuments.Skip(i).Take(batchSize).ToList();

                try
                {
                    _logger.LogInformation("Indexing batch {BatchNum}/{TotalBatches} with {Count} documents",
                        batchNum, totalBatches, batch.Count);

                    // Index this batch
                    await _ragRetrievalService.IndexDocumentsAsync(batch, cancellationToken);
                    indexedCount += batch.Count;

                    _logger.LogInformation("Batch {BatchNum} indexed successfully. Total indexed: {Total}/{All}",
                        batchNum, indexedCount, ragDocuments.Count);

                    // Wait before next batch (except for last batch)
                    if (i + batchSize < ragDocuments.Count)
                    {
                        _logger.LogInformation("Waiting {DelaySeconds}s before next batch to avoid API rate limit...",
                            delayBetweenBatchesSeconds);
                        await Task.Delay(delayBetweenBatchesSeconds * 1000, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Progressive indexing cancelled at batch {BatchNum}", batchNum);
                    return StatusCode(StatusCodes.Status206PartialContent, new
                    {
                        message = "Progressive indexing cancelled",
                        indexedCount = indexedCount,
                        totalCount = ragDocuments.Count,
                        completedBatches = batchNum - 1,
                        totalBatches = totalBatches
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error indexing batch {BatchNum}", batchNum);
                    return StatusCode(StatusCodes.Status206PartialContent, new
                    {
                        message = "Progressive indexing partially completed with errors",
                        indexedCount = indexedCount,
                        totalCount = ragDocuments.Count,
                        failedBatch = batchNum,
                        error = ex.Message,
                        completedBatches = batchNum - 1,
                        totalBatches = totalBatches
                    });
                }
            }

            _logger.LogInformation("Progressive indexing completed successfully. Indexed {Count} documents",
                indexedCount);

            return Ok(new
            {
                message = "Progressive indexing completed successfully",
                indexedCount = indexedCount,
                totalCount = ragDocuments.Count,
                completedBatches = totalBatches,
                breakdown = new
                {
                    programs = ragDocuments.Count(d => d.Metadata?["type"] == "program"),
                    majors = ragDocuments.Count(d => d.Metadata?["type"] == "major"),
                    admissionTypes = ragDocuments.Count(d => d.Metadata?["type"] == "admission_type"),
                    admissionConfigs = ragDocuments.Count(d => d.Metadata?["type"] == "admission_config")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during progressive indexing: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Progressive indexing failed",
                message = ex.Message
            });
        }
    }
}

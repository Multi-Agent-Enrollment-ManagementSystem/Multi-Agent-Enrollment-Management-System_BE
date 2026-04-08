using MAEMS.Application.Interfaces;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using MAEMS.Infrastructure.Repositories;
using MAEMS.Infrastructure.Services;
using MAEMS.MultiAgent.RAG.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.Agents;

/// <summary>
/// ChatBox Agent - Xử lý câu hỏi từ thí sinh về quy chế tuyển sinh
/// Sử dụng Gemini API + RAG để trả lời dựa vào knowledge base, history chat
/// </summary>
public sealed class ChatBoxAgent : IChatBoxAgent
{
    private readonly IGeminiService _geminiService;
    private readonly ILlmChatLogRepositoryLegacy _chatLogRepository;
    private readonly postgresContext _dbContext;
    private readonly IRagRetrievalService _ragRetrievalService;
    private readonly ILogger<ChatBoxAgent> _logger;

    public ChatBoxAgent(
        IGeminiService geminiService,
        ILlmChatLogRepositoryLegacy chatLogRepository,
        postgresContext dbContext,
        IRagRetrievalService ragRetrievalService,
        ILogger<ChatBoxAgent> logger)
    {
        _geminiService = geminiService;
        _chatLogRepository = chatLogRepository;
        _dbContext = dbContext;
        _ragRetrievalService = ragRetrievalService;
        _logger = logger;
    }

    public async Task<string> RespondAsync(
        int userId,
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ChatBoxAgent processing query for user {UserId}", userId);

        try
        {
            // 1. Try to retrieve relevant documents from RAG system
            string ragContext = "No relevant information found.";
            try
            {
                _logger.LogInformation("Starting RAG retrieval for query: {Query}", userQuery);
                ragContext = await _ragRetrievalService.RetrieveAsContextAsync(
                    userQuery,
                    topK: 5,
                    cancellationToken);
                _logger.LogInformation("RAG retrieval completed. Context length: {Length} characters", ragContext.Length);
                _logger.LogDebug("RAG Context: {RagContext}", ragContext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RAG retrieval failed, will use DB-only approach");
                ragContext = ""; // Will trigger fallback to DB-only in prompt building
            }

            // 2. Build system prompt with RAG context + admission rules
            var systemPrompt = await BuildSystemPromptWithRagAsync(ragContext, cancellationToken);
            _logger.LogDebug("System prompt built. Prompt length: {Length} characters", systemPrompt.Length);

            // 3. Get conversation history (last 5 messages)
            var conversationHistory = await GetConversationHistoryAsync(userId, 5, cancellationToken);

            // 4. Call Gemini API
            var llmResponse = await _geminiService.GetResponseAsync(
                userQuery,
                conversationHistory,
                systemPrompt,
                cancellationToken);

            // 5. Save to database
            var chatLog = new LlmChatLog
            {
                UserId = userId,
                UserQuery = userQuery,
                LlmResponse = llmResponse,
                Message = userQuery, // Keep for backward compatibility
                CreatedAt = DateTime.Now  // ← PostgreSQL 'timestamp without time zone' không nhận UTC
            };

            await _chatLogRepository.AddAsync(chatLog, cancellationToken);

            _logger.LogInformation("ChatBoxAgent response saved for user {UserId}", userId);

            return llmResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatBoxAgent.RespondAsync for user {UserId}", userId);
            throw;
        }
    }

    private async Task<string> BuildSystemPromptWithRagAsync(string ragContext, CancellationToken cancellationToken = default)
    {
        try
        {
            // Pure RAG approach - Chỉ sử dụng thông tin từ RAG retrieval
            var systemPrompt = $@"Bạn là chatbot tư vấn tuyển sinh của Trường Đại học.

**KIẾN THỨC CHUYÊN MÔN TỪ HỆ THỐNG (RAG)**
{(string.IsNullOrWhiteSpace(ragContext) ? "❌ Không tìm thấy thông tin liên quan trong hệ thống. Vui lòng liên hệ phòng tuyển sinh để được hỗ trợ." : ragContext)}

**VAI TRÒ VÀ NGUYÊN TẮC**
1. Bạn là trợ lý tư vấn tuyển sinh thân thiện, chuyên nghiệp và chính xác
2. **CHỈ SỬ DỤNG thông tin từ 'KIẾN THỨC CHUYÊN MÔN TỪ HỆ THỐNG (RAG)' ở trên**
3. **TUYỆT ĐỐI KHÔNG tự bịa, suy đoán, hoặc thêm thông tin không có trong RAG context**

**PHẠM VI TƯ VẤN**
Trả lời câu hỏi về:
- Các ngành học và chương trình tuyển sinh
- Điều kiện tuyển sinh cho từng phương thức
- Yêu cầu tài liệu cần nộp
- Quy trình nộp hồ sơ online
- Thông tin liên hệ (hotline, email, địa chỉ campus)
- Thời gian công bố kết quả
- Các chính sách đặc biệt (ưu tiên, xét tuyển bổ sung...)
- Thông tin về học phí và hỗ trợ tài chính

**HƯỚNG DẪN TRẢ LỜI**
✅ Khi có thông tin trong RAG:
   - Trả lời chi tiết, chính xác dựa trên RAG context
   - Trích dẫn thông tin cụ thể từ tài liệu
   - Sử dụng ngôn ngữ Tiếng Việt thân thiện và dễ hiểu
   - Cấu trúc câu trả lời rõ ràng (bullet points, đánh số)

❌ Khi KHÔNG có thông tin trong RAG:
   - Trả lời: ""Xin lỗi, tôi không tìm thấy thông tin về [chủ đề] trong hệ thống của tôi. Vui lòng liên hệ trực tiếp với phòng tuyển sinh để được hỗ trợ chi tiết.""
   - **KHÔNG đưa ra thông tin từ kiến thức chung**
   - **KHÔNG tự bịa số hotline, email, địa chỉ**

⛔ Các câu hỏi ngoài lĩnh vực tuyển sinh:
   - ""Xin lỗi, tôi chỉ có thể tư vấn về tuyển sinh. Vui lòng liên hệ với phòng tuyển sinh để được hỗ trợ về các vấn đề khác.""

**QUY TẮC QUAN TRỌNG**
❗ KHÔNG đưa ra quyết định cuối cùng về tuyển sinh (chỉ là tư vấn)
❗ KHÔNG tự suy luận hoặc thêm thông tin không có trong RAG
❗ KHÔNG cung cấp thông tin liên hệ giả (chỉ dùng nếu có trong RAG)
❗ Luôn khuyến khích thí sinh làm theo quy trình chính thức";

            return systemPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system prompt with RAG");
            // Fallback to basic prompt without RAG context
            return await BuildSystemPromptAsync(cancellationToken);
        }
    }

    private async Task<string> BuildSystemPromptAsync(CancellationToken cancellationToken = default)
         {
             try
             {
                 // Lấy thông tin về các chương trình tuyển sinh từ DB
                 var programs = await _dbContext.Programs
                     .Where(p => p.IsActive == true)
                     .Select(p => new { p.ProgramName, p.Description })
                     .Take(20)
                     .ToListAsync(cancellationToken);

                 var majors = await _dbContext.Majors
                     .Where(m => m.IsActive == true)
                     .Select(m => new { m.MajorName })
                     .Take(30)
                     .ToListAsync(cancellationToken);

                 var admissionTypes = await _dbContext.AdmissionTypes
                     .Where(a => a.IsActive == true)
                     .Select(a => new { a.AdmissionTypeName, a.Type })
                     .ToListAsync(cancellationToken);


                 var programsText = string.Join("\n", programs.Select(p => $"- {p.ProgramName}: {p.Description}"));
                 var majorsText = string.Join(", ", majors.Select(m => m.MajorName));
                 var admissionTypesText = string.Join("\n", admissionTypes.Select(a =>
                     $"- {a.AdmissionTypeName} ({a.Type})"));

                 var systemPrompt = $@"Bạn là chatbot tư vấn tuyển sinh của Trường Đại học.

    **THÔNG TIN HỆ THỐNG TUYỂN SINH**

    Năm học hiện tại: {DateTime.Now.Year}

**Các chương trình tuyển sinh:**
{(string.IsNullOrWhiteSpace(programsText) ? "- Các chương trình sẽ được cập nhật sớm" : programsText)}

**Các ngành học:**
{(string.IsNullOrWhiteSpace(majorsText) ? "- Các ngành sẽ được cập nhật sớm" : majorsText)}

**Phương thức xét tuyển:**
{(string.IsNullOrWhiteSpace(admissionTypesText) ? "- Các phương thức sẽ được cập nhật sớm" : admissionTypesText)}

**HƯỚNG DẪN HOẠT ĐỘNG**
1. Bạn là trợ lý tư vấn tuyển sinh thân thiện và chuyên nghiệp
2. Trả lời câu hỏi về:
   - Các ngành học và chương trình tuyển sinh
   - Điều kiện tuyển sinh cho từng phương thức
   - Yêu cầu tài liệu cần nộp
   - Quy trình nộp hồ sơ online
   - Thời gian công bố kết quả
   - Các chính sách đặc biệt (ưu tiên, xét tuyển bổ sung...)

3. Khi trả lời:
   - Sử dụng ngôn ngữ Tiếng Việt, thân thiện và dễ hiểu
   - Cung cấp thông tin chi tiết và chính xác từ dữ liệu phía trên
   - Nếu thí sinh hỏi về tài liệu, hãy liệt kê đầy đủ
   - Khuyến khích thí sinh nộp hồ sơ sớm để tránh chậm trễ

4. Các câu hỏi ngoài lĩnh vực tuyển sinh:
   - Từ chối lịch sự: ""Xin lỗi, tôi chỉ có thể tư vấn về tuyển sinh. Vui lòng liên hệ với phòng tuyển sinh để được hỗ trợ thêm.""

5. Nếu không biết thông tin:
   - Gợi ý: ""Xin lỗi, tôi không tìm thấy thông tin này. Vui lòng liên hệ phòng tuyển sinh để được hỗ trợ cụ thể.""

**LƯU Ý QUAN TRỌNG**
- Luôn khuyến khích thí sinh làm theo quy trình chính thức
- Không đưa ra quyết định cuối cùng về tuyển sinh (chỉ là tư vấn)
- **CHỈ sử dụng thông tin có trong dữ liệu phía trên - KHÔNG tự bịa số hotline, email, hoặc địa chỉ**";

            return systemPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system prompt");
            // Return minimal prompt if DB query fails
            return @"Bạn là chatbot tư vấn tuyển sinh. Hãy trả lời câu hỏi về các chương trình, ngành học, điều kiện tuyển sinh, yêu cầu tài liệu, và quy trình nộp hồ sơ.
Trả lời bằng Tiếng Việt, thân thiện và chuyên nghiệp. Nếu câu hỏi ngoài lĩnh vực tuyển sinh, hãy từ chối lịch sự. KHÔNG tự bịa thông tin liên hệ.";
        }
    }

    private async Task<List<(string role, string content)>> GetConversationHistoryAsync(
        int userId,
        int maxMessages = 5,
        CancellationToken cancellationToken = default)
    {
        var history = new List<(string role, string content)>();

        try
        {
            // Lấy lịch sử chat gần đây
            var chatLogs = await _chatLogRepository.GetByUserIdAsync(
                userId,
                pageNumber: 1,
                pageSize: maxMessages,
                cancellationToken);

            // Đảo ngược để có thứ tự chronological
            chatLogs.Reverse();

            foreach (var log in chatLogs)
            {
                if (!string.IsNullOrWhiteSpace(log.UserQuery))
                    history.Add(("user", log.UserQuery));

                if (!string.IsNullOrWhiteSpace(log.LlmResponse))
                    history.Add(("assistant", log.LlmResponse));
            }

            _logger.LogInformation("Retrieved {Count} messages from conversation history for user {UserId}",
                history.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving conversation history for user {UserId}", userId);
            // Return empty history if retrieval fails - conversation will continue without context
        }

        return history;
    }
}

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

**LIÊN HỆ HỖ TRỢ TUYỂN SINH**
- Hotline: 1900-1234-567 (Mở 8:00 - 17:00, Thứ 2 - Thứ 6)
- Email: tuyen.sinh@university.edu.vn
- Website: https://admissions.university.edu.vn
- Địa chỉ: 123 Đường Tuyển Sinh, TP. Hồ Chí Minh

**THÔNG TIN CHUYÊN SÂU TỪ HỆ THỐNG**
{(string.IsNullOrWhiteSpace(ragContext) ? "(Đang sử dụng DB-only mode)" : ragContext)}

**HƯỚNG DẪN HOẠT ĐỘNG**
1. Bạn là trợ lý tư vấn tuyển sinh thân thiện và chuyên nghiệp
2. Sử dụng thông tin từ hệ thống RAG (ở trên) để trả lời câu hỏi chính xác
3. Trả lời câu hỏi về:
   - Các ngành học và chương trình tuyển sinh
   - Điều kiện tuyển sinh cho từng phương thức
   - Yêu cầu tài liệu cần nộp
   - Quy trình nộp hồ sơ online
   - Thời gian công bố kết quả
   - Các chính sách đặc biệt (ưu tiên, xét tuyển bổ sung...)
   - Thông tin về học phí và hỗ trợ tài chính

4. Khi trả lời:
   - Sử dụng ngôn ngữ Tiếng Việt, thân thiện và dễ hiểu
   - Cung cấp thông tin chi tiết và chính xác từ knowledge base
   - Nếu thí sinh hỏi về tài liệu, hãy liệt kê đầy đủ
   - Khuyến khích thí sinh nộp hồ sơ sớm để tránh chậm trễ
   - **Luôn kết thúc bằng cách cung cấp liên hệ hỗ trợ (hotline/email/website)**

5. Các câu hỏi ngoài lĩnh vực tuyển sinh:
   - Từ chối lịch sự: ""Xin lỗi, tôi chỉ có thể tư vấn về tuyển sinh. Vui lòng liên hệ với phòng tuyển sinh để được hỗ trợ thêm. Hotline: 1900-1234-567 hoặc email: tuyen.sinh@university.edu.vn""

6. Nếu không biết thông tin từ knowledge base:
   - Gợi ý: ""Vui lòng gọi hotline tư vấn tuyển sinh (1900-1234-567) hoặc truy cập website https://admissions.university.edu.vn để cập nhật thông tin mới nhất. Email hỗ trợ: tuyen.sinh@university.edu.vn""

**LƯU Ý QUAN TRỌNG**
- Luôn khuyến khích thí sinh làm theo quy trình chính thức
- Cung cấp liên hệ hotline khi cần hỗ trợ thêm
- Không đưa ra quyết định cuối cùng về tuyển sinh (chỉ là tư vấn)
- Ưu tiên sử dụng thông tin từ knowledge base thay vì thông tin chung chung
- **Khi kết thúc, gợi ý thí sinh liên hệ để được hỗ trợ thêm**";

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

**LIÊN HỆ HỖ TRỢ TUYỂN SINH**
- Hotline: 1900-1234-567 (Mở 8:00 - 17:00, Thứ 2 - Thứ 6)
- Email: tuyen.sinh@university.edu.vn
- Website: https://admissions.university.edu.vn
- Địa chỉ: 123 Đường Tuyển Sinh, TP. Hồ Chí Minh

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
   - Cung cấp thông tin chi tiết và chính xác
   - Nếu thí sinh hỏi về tài liệu, hãy liệt kê đầy đủ
   - Khuyến khích thí sinh nộp hồ sơ sớm để tránh chậm trễ
   - **Luôn kết thúc bằng cách cung cấp liên hệ hỗ trợ (hotline/email/website)**

4. Các câu hỏi ngoài lĩnh vực tuyển sinh:
   - Từ chối lịch sự: ""Xin lỗi, tôi chỉ có thể tư vấn về tuyển sinh. Vui lòng liên hệ với phòng tuyển sinh để được hỗ trợ thêm. Hotline: 1900-1234-567 hoặc email: tuyen.sinh@university.edu.vn""

5. Nếu không biết thông tin:
   - Gợi ý: ""Vui lòng gọi hotline tư vấn tuyển sinh (1900-1234-567) hoặc truy cập website https://admissions.university.edu.vn để cập nhật thông tin mới nhất. Email hỗ trợ: tuyen.sinh@university.edu.vn""

**LƯU Ý QUAN TRỌNG**
- Luôn khuyến khích thí sinh làm theo quy trình chính thức
- Cung cấp liên hệ hotline khi cần hỗ trợ thêm
- Không đưa ra quyết định cuối cùng về tuyển sinh (chỉ là tư vấn)
- **Khi kết thúc, gợi ý thí sinh liên hệ để được hỗ trợ thêm**";

            return systemPrompt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building system prompt");
            // Return minimal prompt if DB query fails
            return @"Bạn là chatbot tư vấn tuyển sinh. Hãy trả lời câu hỏi về các chương trình, ngành học, điều kiện tuyển sinh, yêu cầu tài liệu, và quy trình nộp hồ sơ.
Trả lời bằng Tiếng Việt, thân thiện và chuyên nghiệp. Nếu câu hỏi ngoài lĩnh vực tuyển sinh, hãy từ chối lịch sự.";
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

using MAEMS.Application.Interfaces;
using MAEMS.Application.Services;
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
/// Sử dụng OpenAI GPT-4 cho chat + Gemini embeddings + RAG
/// </summary>
public sealed class ChatBoxAgent : IChatBoxAgent
{
    private readonly IOpenAIService _openAIService;
    private readonly ILlmChatLogRepositoryLegacy _chatLogRepository;
    private readonly postgresContext _dbContext;
    private readonly IRagRetrievalService _ragRetrievalService;
    private readonly TuitionFeeService _tuitionFeeService;
    private readonly ILogger<ChatBoxAgent> _logger;

    public ChatBoxAgent(
        IOpenAIService openAIService,
        ILlmChatLogRepositoryLegacy chatLogRepository,
        postgresContext dbContext,
        IRagRetrievalService ragRetrievalService,
        TuitionFeeService tuitionFeeService,
        ILogger<ChatBoxAgent> logger)
    {
        _openAIService = openAIService;
        _chatLogRepository = chatLogRepository;
        _dbContext = dbContext;
        _ragRetrievalService = ragRetrievalService;
        _tuitionFeeService = tuitionFeeService;
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

            // 2. Build tuition fee knowledge context (if query is tuition-related)
            var tuitionContext = await BuildTuitionContextAsync(userQuery, cancellationToken);

            // 3. Build system prompt with RAG context + tuition context + admission rules
            var systemPrompt = await BuildSystemPromptWithRagAsync(ragContext, tuitionContext, cancellationToken);
            _logger.LogDebug("System prompt built. Prompt length: {Length} characters", systemPrompt.Length);

            // 4. Get conversation history (last 5 messages)
            var conversationHistory = await GetConversationHistoryAsync(userId, 5, cancellationToken);

            // 5. Call OpenAI API (Gemini still used for embeddings in RAG)
            var llmResponse = await _openAIService.GetChatCompletionAsync(
                systemPrompt,
                userQuery,
                conversationHistory,
                cancellationToken);

            // 6. Save to database
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

    private async Task<string> BuildTuitionContextAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if query is tuition-related
            var tuitionKeywords = new[] { "học phí", "tuition", "chi phí", "tiền học", "phí", "học kỳ", "campus", "khu vực" };
            var isTuitionQuery = tuitionKeywords.Any(keyword =>
                userQuery.Contains(keyword, StringComparison.OrdinalIgnoreCase));

            if (!isTuitionQuery)
            {
                return string.Empty; // Not a tuition question
            }

            _logger.LogInformation("Detected tuition-related query, building tuition context");

            // Smart campus detection from query
            var queryLower = userQuery.ToLower();
            var requestedCampuses = new List<string>();

            if (queryLower.Contains("hà nội") || queryLower.Contains("ha noi") || queryLower.Contains("hanoi"))
                requestedCampuses.Add("Hà Nội");
            if (queryLower.Contains("hồ chí minh") || queryLower.Contains("hcm") || queryLower.Contains("sài gòn") || queryLower.Contains("saigon"))
                requestedCampuses.Add("TP. Hồ Chí Minh");
            if (queryLower.Contains("đà nẵng") || queryLower.Contains("da nang") || queryLower.Contains("danang"))
                requestedCampuses.Add("Đà Nẵng");
            if (queryLower.Contains("cần thơ") || queryLower.Contains("can tho") || queryLower.Contains("cantho"))
                requestedCampuses.Add("Cần Thơ");
            if (queryLower.Contains("quy nhơn") || queryLower.Contains("quy nhon") || queryLower.Contains("quynhon"))
                requestedCampuses.Add("Quy Nhơn");

            // Detect if user explicitly asks for KV1 pricing
            var isKv1Query = queryLower.Contains("kv1") || 
                           queryLower.Contains("kv 1") ||
                           queryLower.Contains("khu vực 1") || 
                           queryLower.Contains("khu vuc 1") ||
                           queryLower.Contains("ưu tiên 1") ||
                           queryLower.Contains("uu tien 1") ||
                           queryLower.Contains("vùng ưu tiên") ||
                           queryLower.Contains("vung uu tien");

            // Get all active tuition fees from database
            var allFees = await _dbContext.TuitionFees
                .Where(f => f.IsActive == true && f.FeeType == "REGULAR")
                .OrderBy(f => f.CampusName)
                .ThenBy(f => f.MajorName)
                .ThenBy(f => f.Region)
                .ToListAsync(cancellationToken);

            if (!allFees.Any())
            {
                return "⚠️ Hiện tại chưa có thông tin học phí trong hệ thống.";
            }

            // Filter by requested campuses if specific campus mentioned
            var relevantFees = requestedCampuses.Any()
                ? allFees.Where(f => requestedCampuses.Contains(f.CampusName ?? "")).ToList()
                : allFees; // If no specific campus, show all (for comparison questions)

            if (requestedCampuses.Any() && !relevantFees.Any())
            {
                return $"⚠️ Không tìm thấy thông tin học phí cho campus: {string.Join(", ", requestedCampuses)}";
            }

            // Build context
            var tuitionSummary = new System.Text.StringBuilder();
            tuitionSummary.AppendLine("📊 **THÔNG TIN HỌC PHÍ TẠI CÁC CAMPUS**");
            tuitionSummary.AppendLine();

            // CRITICAL: Add region detection result
            if (isKv1Query)
            {
                tuitionSummary.AppendLine("🔍 **PHÁT HIỆN:** Người dùng HỎI RÕ về học phí KHU VỰC ƯU TIÊN 1 (KV1)");
                tuitionSummary.AppendLine("➡️ **HÀNH ĐỘNG:** Trả lời theo giá KV1");
            }
            else
            {
                tuitionSummary.AppendLine("🔍 **PHÁT HIỆN:** Người dùng KHÔNG nêu rõ khu vực ưu tiên");
                tuitionSummary.AppendLine("➡️ **HÀNH ĐỘNG BẮT BUỘC:** Trả lời theo giá 'Các khu vực khác' (OTHER - giá cao hơn)");
                tuitionSummary.AppendLine("➡️ **BỔ SUNG:** Có thể thêm 'Nếu bạn thuộc KV1 thì học phí sẽ thấp hơn'");
            }
            tuitionSummary.AppendLine();

            // Get orientation and English prep fees for requested campuses
            var orientationFees = new Dictionary<string, (decimal Kv1, decimal Other)>();
            var englishPrepFees = new Dictionary<string, (decimal Kv1, decimal Other)>();

            var campusList = requestedCampuses.Any() 
                ? requestedCampuses 
                : new List<string> { "Hà Nội", "TP. Hồ Chí Minh", "Đà Nẵng", "Cần Thơ", "Quy Nhơn" };

            foreach (var campus in campusList)
            {
                var orientKv1 = await _tuitionFeeService.GetOrientationFeeAsync(campus, "KV1");
                var orientOther = await _tuitionFeeService.GetOrientationFeeAsync(campus, "OTHER");
                if (orientKv1.HasValue && orientOther.HasValue)
                {
                    orientationFees[campus] = (orientKv1.Value, orientOther.Value);
                }

                var englishKv1 = await _tuitionFeeService.GetEnglishPrepFeeAsync(campus, "KV1");
                var englishOther = await _tuitionFeeService.GetEnglishPrepFeeAsync(campus, "OTHER");
                if (englishKv1.HasValue && englishOther.HasValue)
                {
                    englishPrepFees[campus] = (englishKv1.Value, englishOther.Value);
                }
            }

            var campusGroups = relevantFees
                .GroupBy(f => f.CampusName)
                .OrderBy(g => g.Key);

            foreach (var campusGroup in campusGroups)
            {
                var campusName = campusGroup.Key ?? "Unknown";
                var firstFee = campusGroup.First();
                var discount = firstFee.CampusDiscountPercent;

                tuitionSummary.AppendLine($"🏢 **Campus: {campusName}** (Ưu đãi vùng miền: {discount}%)");
                tuitionSummary.AppendLine();

                // Add orientation and English prep fees
                if (orientationFees.ContainsKey(campusName))
                {
                    var orient = orientationFees[campusName];
                    tuitionSummary.AppendLine($"  🎓 **Học phí định hướng (ORIENTATION):**");

                    if (isKv1Query)
                    {
                        tuitionSummary.AppendLine($"    - **KV1: {orient.Kv1:N0} VND**");
                        tuitionSummary.AppendLine($"    - Các khu vực khác (tham khảo): {orient.Other:N0} VND");
                    }
                    else
                    {
                        tuitionSummary.AppendLine($"    - **Các khu vực khác: {orient.Other:N0} VND**");
                        tuitionSummary.AppendLine($"    - Nếu thuộc KV1: {orient.Kv1:N0} VND");
                    }
                    tuitionSummary.AppendLine($"    - Ghi chú: Tân sinh viên nhập học phải nộp 1 lần");
                    tuitionSummary.AppendLine();
                }

                if (englishPrepFees.ContainsKey(campusName))
                {
                    var english = englishPrepFees[campusName];
                    tuitionSummary.AppendLine($"  📝 **Học phí tiếng Anh dự bị (ENGLISH PREP - mỗi mức):**");

                    if (isKv1Query)
                    {
                        tuitionSummary.AppendLine($"    - **KV1: {english.Kv1:N0} VND/mức**");
                        tuitionSummary.AppendLine($"    - Các khu vực khác (tham khảo): {english.Other:N0} VND/mức");
                    }
                    else
                    {
                        tuitionSummary.AppendLine($"    - **Các khu vực khác: {english.Other:N0} VND/mức**");
                        tuitionSummary.AppendLine($"    - Nếu thuộc KV1: {english.Kv1:N0} VND/mức");
                    }
                    tuitionSummary.AppendLine($"    - Ghi chú: Tối đa 6 mức, miễn nếu có IELTS 6.0+ hoặc tương đương");
                    tuitionSummary.AppendLine();
                }

                // Group by major category
                tuitionSummary.AppendLine($"  💼 **Học phí chuyên ngành (REGULAR):**");
                var majorGroups = campusGroup
                    .Where(f => f.FeeType == "REGULAR")
                    .GroupBy(f => f.MajorName)
                    .Take(requestedCampuses.Any() ? 20 : 8); // More details if specific campus requested

                foreach (var majorGroup in majorGroups)
                {
                    var majorName = majorGroup.Key ?? "Unknown";
                    var kv1Fee = majorGroup.FirstOrDefault(f => f.Region == "KV1");
                    var otherFee = majorGroup.FirstOrDefault(f => f.Region == "OTHER");

                    if (kv1Fee != null && otherFee != null)
                    {
                        tuitionSummary.AppendLine($"    • **{majorName}**");

                        // Calculate semester fees with growth (using helper method for Infrastructure.Models.TuitionFee)
                        var kv1Hk1 = CalculateSemesterFeeForModel(kv1Fee, 1);
                        var kv1Hk4 = CalculateSemesterFeeForModel(kv1Fee, 4);
                        var kv1Hk7 = CalculateSemesterFeeForModel(kv1Fee, 7);

                        var otherHk1 = CalculateSemesterFeeForModel(otherFee, 1);
                        var otherHk4 = CalculateSemesterFeeForModel(otherFee, 4);
                        var otherHk7 = CalculateSemesterFeeForModel(otherFee, 7);

                        // STRATEGY: Show pricing based on KV1 detection
                        if (isKv1Query)
                        {
                            // User asked for KV1 specifically - show KV1 first, OTHER as reference
                            tuitionSummary.AppendLine($"      - **KV1 (Khu vực ưu tiên 1):**");
                            tuitionSummary.AppendLine($"        HK1-3: {kv1Hk1:N0} VND/kỳ");
                            tuitionSummary.AppendLine($"        HK4-6: {kv1Hk4:N0} VND/kỳ (tăng 6.3% so với HK1)");
                            tuitionSummary.AppendLine($"        HK7-9: {kv1Hk7:N0} VND/kỳ (tăng 6.5% so với HK4)");

                            var totalKv1 = CalculateTotalEstimateForModel(kv1Fee, 9);
                            tuitionSummary.AppendLine($"        **Tổng 9 học kỳ: {totalKv1:N0} VND**");
                            tuitionSummary.AppendLine();

                            tuitionSummary.AppendLine($"      - Các khu vực khác (tham khảo): {otherHk1:N0} VND/kỳ");
                        }
                        else
                        {
                            // User did NOT ask for KV1 - show OTHER first and prominently
                            tuitionSummary.AppendLine($"      - **Các khu vực khác (GIÁ TIÊU CHUẨN):**");
                            tuitionSummary.AppendLine($"        HK1-3: {otherHk1:N0} VND/kỳ");
                            tuitionSummary.AppendLine($"        HK4-6: {otherHk4:N0} VND/kỳ (tăng 6.3% so với HK1)");
                            tuitionSummary.AppendLine($"        HK7-9: {otherHk7:N0} VND/kỳ (tăng 6.5% so với HK4)");

                            var totalOther = CalculateTotalEstimateForModel(otherFee, 9);
                            tuitionSummary.AppendLine($"        **Tổng 9 học kỳ: {totalOther:N0} VND**");
                            tuitionSummary.AppendLine();

                            tuitionSummary.AppendLine($"      - Nếu thuộc KV1 (khu vực ưu tiên 1): {kv1Hk1:N0} VND/kỳ");
                        }
                    }
                }

                tuitionSummary.AppendLine();
            }

            // Add important notes
            tuitionSummary.AppendLine("📌 **QUY ĐỊNH TĂNG HỌC PHÍ CHUYÊN NGÀNH (REGULAR - 9 HỌC KỲ):**");
            tuitionSummary.AppendLine("- **HK1-3:** Giá gốc (base amount)");
            tuitionSummary.AppendLine("- **HK4-6:** Tăng 6.3% so với HK1");
            tuitionSummary.AppendLine("- **HK7-9:** Tăng 6.5% so với HK4 (tức là tăng 13.2% so với HK1)");
            tuitionSummary.AppendLine();
            tuitionSummary.AppendLine("📌 **LƯU Ý QUAN TRỌNG:**");
            tuitionSummary.AppendLine("- Học phí trên áp dụng cho tân sinh viên K22 (năm 2026)");
            tuitionSummary.AppendLine("- KV1: Khu vực ưu tiên 1 (theo quy định Bộ GD&ĐT)");
            tuitionSummary.AppendLine("- **Ưu đãi vùng miền:** Quy Nhơn (50%), Đà Nẵng/Cần Thơ (30%), Hà Nội/HCM (0%)");
            tuitionSummary.AppendLine("- **Học phí định hướng:** Nộp 1 lần khi nhập học");
            tuitionSummary.AppendLine("- **Học phí tiếng Anh:** Sinh viên có IELTS 6.0+ được miễn, các sinh viên khác xếp lớp theo trình độ");
            tuitionSummary.AppendLine();
            tuitionSummary.AppendLine("⚠️ **QUAN TRỌNG:** Khi trả lời:");
            tuitionSummary.AppendLine("- Luôn NÊU RÕ TÊN CAMPUS trong câu trả lời");
            tuitionSummary.AppendLine("- Phân biệt rõ giá KV1 vs Các khu vực khác");
            tuitionSummary.AppendLine("- Giải thích ưu đãi vùng miền nếu có");
            tuitionSummary.AppendLine("- Khi hỏi về tăng học phí: SỬ DỤNG ĐÚNG tỷ lệ tăng đã cung cấp (6.3% HK4, 6.5% HK7)");

            return tuitionSummary.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building tuition context");
            return "⚠️ Có lỗi khi truy xuất thông tin học phí.";
        }
    }

    private async Task<string> BuildSystemPromptWithRagAsync(
        string ragContext,
        string tuitionContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Combine RAG context with tuition context
            var combinedContext = new System.Text.StringBuilder();

            if (!string.IsNullOrWhiteSpace(ragContext))
            {
                combinedContext.AppendLine("📚 **TÀI LIỆU TUYỂN SINH**");
                combinedContext.AppendLine(ragContext);
                combinedContext.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(tuitionContext))
            {
                combinedContext.AppendLine(tuitionContext);
            }

            var finalContext = combinedContext.Length > 0
                ? combinedContext.ToString()
                : "❌ Không tìm thấy thông tin liên quan trong hệ thống. Vui lòng liên hệ phòng tuyển sinh để được hỗ trợ.";

            // Pure RAG approach - Chỉ sử dụng thông tin từ RAG retrieval
            var systemPrompt = $@"Bạn là chatbot tư vấn tuyển sinh của Trường Đại học.

**KIẾN THỨC CHUYÊN MÔN TỪ HỆ THỐNG (RAG)**
{finalContext}

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
- **Thông tin về học phí theo campus, ngành học, khu vực và học kỳ**

**HƯỚNG DẪN TRẢ LỜI VỀ HỌC PHÍ**

🎓 **LOẠI HỌC PHÍ:**
1. **Học phí định hướng (ORIENTATION):**
   - Nộp 1 lần khi nhập học
   - KV1: 9.170.000 VND, Các khu vực khác: 13.100.000 VND
   - Áp dụng giảm giá theo campus (Đà Nẵng/Cần Thơ: -30%, Quy Nhơn: -50%)

2. **Học phí tiếng Anh dự bị (ENGLISH_PREP):**
   - Giá mỗi mức: KV1: 9.170.000 VND, Các khu vực khác: 13.100.000 VND
   - Tối đa 6 mức, sinh viên có IELTS 6.0+ được miễn
   - Áp dụng giảm giá theo campus

3. **Học phí chuyên ngành (REGULAR - 9 học kỳ):**
   - Có quy định tăng học phí theo học kỳ:
     • HK1-3: Giá gốc
     • HK4-6: Tăng 6.3% so với HK1
     • HK7-9: Tăng 6.5% so với HK4 (tức 13.2% so với HK1)

✅ **QUY TẮC TRẢ LỜI KHU VỰC - TUYỆT ĐỐI BẮT BUỘC:**
   1. **KIỂM TRA PHÁT HIỆN KHU VỰC Ở ĐẦU CONTEXT:**
      - Nếu thấy ""HÀNH ĐỘNG: Trả lời theo giá KV1"" → Trả lời giá KV1
      - Nếu thấy ""HÀNH ĐỘNG BẮT BUỘC: Trả lời theo giá 'Các khu vực khác'"" → **BẮT BUỘC trả lời giá OTHER**

   2. **MẶC ĐỊNH = OTHER (các khu vực khác - giá cao hơn):**
      - Khi không phát hiện KV1 → LUÔN trả lời giá OTHER trước
      - Có thể thêm: ""Nếu bạn thuộc KV1 (khu vực ưu tiên 1), học phí sẽ thấp hơn: [giá KV1]""

   3. **KHI NÀO TRẢ LỜI GIÁ KV1:**
      - CHỈ KHI context có ""HÀNH ĐỘNG: Trả lời theo giá KV1""
      - HOẶC khi người dùng hỏi: ""học phí KV1 là bao nhiêu"", ""giá khu vực 1"", ""ưu tiên 1""

✅ **KHI TRẢ LỜI VỀ HỌC PHÍ - BẮT BUỘC:**
   - Luôn NÊU RÕ TÊN CAMPUS trong câu trả lời (VD: ""tại campus Đà Nẵng"", ""ở campus Hà Nội"")
   - Luôn NÊU RÕ KHU VỰC: ""Các khu vực khác"" hoặc ""Khu vực ưu tiên 1 (KV1)""
   - Nếu không chắc chắn → ưu tiên giá OTHER để tránh gây hiểu nhầm
   - Đơn vị tiền tệ: VND hoặc VNĐ
   - Nêu rõ loại học phí (định hướng/tiếng Anh/chuyên ngành)
   - Có thể thêm: ""Nếu bạn thuộc KV1 (khu vực ưu tiên 1), học phí sẽ là [số tiền KV1]""

✅ **KHI HỎI VỀ TĂNG HỌC PHÍ CHUYÊN NGÀNH:**
   - SỬ DỤNG ĐÚNG tỷ lệ tăng: HK4 tăng 6.3%, HK7 tăng 6.5%
   - Nêu rõ học phí từng giai đoạn nếu người dùng hỏi
   - VD: ""HK1-3: 22.120.000 VND, HK4-6: 23.513.560 VND (tăng 6.3%), HK7-9: 25.041.941 VND (tăng 6.5% so với HK4)""

✅ **KHI HỎI VỀ TỔNG CHI PHÍ:**
   - Tính đầy đủ: Định hướng + Tiếng Anh (nếu có) + Học phí chuyên ngành 9 kỳ
   - Học phí chuyên ngành: (HK1×3) + (HK4×3) + (HK7×3)
   - Nêu rõ: ""Chưa bao gồm chi phí sinh hoạt, sách vở, và các chi phí khác""

📋 **VÍ DỤ TRẢ LỜI:**
   ❌ **SAI - Trả lời KV1 khi không được hỏi:**
      Câu hỏi: ""Học phí CNTT tại Đà Nẵng là bao nhiêu?""
      Trả lời SAI: ""Học phí CNTT tại Đà Nẵng là 15.480.000 VND/kỳ"" ← SAI vì đây là giá KV1

   ✅ **ĐÚNG - Trả lời OTHER khi không được hỏi rõ KV1:**
      Câu hỏi: ""Học phí CNTT tại Đà Nẵng là bao nhiêu?""
      Trả lời ĐÚNG: ""Học phí CNTT tại Đà Nẵng (các khu vực khác) là 22.120.000 VND/kỳ. Nếu bạn thuộc khu vực ưu tiên 1 (KV1), học phí sẽ là 15.480.000 VND/kỳ.""

   ✅ **ĐÚNG - Trả lời KV1 khi được hỏi rõ:**
      Câu hỏi: ""Học phí CNTT tại Đà Nẵng cho KV1 là bao nhiêu?""
      Trả lời ĐÚNG: ""Học phí CNTT tại Đà Nẵng cho khu vực ưu tiên 1 (KV1) là 15.480.000 VND/kỳ.""

❌ **KHÔNG ĐƯỢC:**
   - ❌ Trả lời giá KV1 khi người dùng không nêu rõ khu vực ưu tiên
   - ❌ Tự động giả định người dùng thuộc KV1
   - Tự bịa tỷ lệ tăng học phí khác với 6.3% và 6.5%
   - Bỏ qua học phí định hướng hoặc tiếng Anh khi tính tổng chi phí
   - Trộn lẫn học phí giữa các campus

**HƯỚNG DẪN TRẢ LỜI CHUNG**
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

    /// <summary>
    /// Calculate semester fee for Infrastructure.Models.TuitionFee with growth rules
    /// </summary>
    private decimal CalculateSemesterFeeForModel(Infrastructure.Models.TuitionFee baseFee, int semesterNumber)
    {
        if (semesterNumber < 1)
            throw new ArgumentException("Semester number must be greater than 0", nameof(semesterNumber));

        decimal amount = baseFee.BaseAmount;

        // Apply campus discount first
        if (baseFee.CampusDiscountPercent.HasValue && baseFee.CampusDiscountPercent.Value > 0)
        {
            amount = amount * (1 - baseFee.CampusDiscountPercent.Value / 100);
        }

        // Apply semester increase rules
        if (semesterNumber >= 1 && semesterNumber <= 3)
        {
            // HK1-3: No increase
            return Math.Round(amount, 0);
        }
        else if (semesterNumber >= 4 && semesterNumber <= 6)
        {
            // HK4-6: Increase 6.3%
            return Math.Round(amount * 1.063m, 0);
        }
        else // semesterNumber >= 7
        {
            // HK7+: First apply 6.3%, then apply 6.5% on the increased amount
            decimal hk4Amount = amount * 1.063m;
            return Math.Round(hk4Amount * 1.065m, 0);
        }
    }

    /// <summary>
    /// Calculate total estimate for Infrastructure.Models.TuitionFee (9 semesters by default for FPT University)
    /// </summary>
    private decimal CalculateTotalEstimateForModel(Infrastructure.Models.TuitionFee baseFee, int totalSemesters = 9)
    {
        decimal total = 0;

        for (int sem = 1; sem <= totalSemesters; sem++)
        {
            total += CalculateSemesterFeeForModel(baseFee, sem);
        }

        return total;
    }
}

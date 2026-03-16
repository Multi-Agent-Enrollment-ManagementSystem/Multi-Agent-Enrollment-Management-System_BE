namespace MAEMS.Application.DTOs.Chat;

/// <summary>
/// DTO cho request hỏi chatbox
/// </summary>
public class AskChatBoxRequestDto
{
    /// <summary>
    /// Câu hỏi của thí sinh
    /// </summary>
    public required string Question { get; set; }
}

/// <summary>
/// DTO cho response từ chatbox
/// </summary>
public class ChatBoxResponseDto
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public int ChatId { get; set; }

    /// <summary>
    /// Câu hỏi của thí sinh
    /// </summary>
    public required string Question { get; set; }

    /// <summary>
    /// Trả lời từ AI
    /// </summary>
    public required string Answer { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO cho một message trong chat history
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// ID của cuộc hội thoại
    /// </summary>
    public int ChatId { get; set; }

    /// <summary>
    /// Câu hỏi
    /// </summary>
    public required string Question { get; set; }

    /// <summary>
    /// Trả lời
    /// </summary>
    public required string Answer { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO cho danh sách chat history
/// </summary>
public class ChatHistoryResponseDto
{
    /// <summary>
    /// Danh sách các messages
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();

    /// <summary>
    /// Tổng số messages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Số trang hiện tại
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Số trang tối đa
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Số messages trên mỗi trang
    /// </summary>
    public int PageSize { get; set; }
}

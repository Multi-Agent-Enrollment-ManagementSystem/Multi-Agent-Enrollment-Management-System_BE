namespace MAEMS.Application.Interfaces;

/// <summary>
/// Interface cho ChatBox Agent
/// </summary>
public interface IChatBoxAgent
{
    /// <summary>
    /// Xử lý câu hỏi từ thí sinh và trả lời dựa vào Gemini AI
    /// </summary>
    /// <param name="userId">ID của thí sinh</param>
    /// <param name="userQuery">Câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Trả lời từ AI</returns>
    Task<string> RespondAsync(
        int userId,
        string userQuery,
        CancellationToken cancellationToken = default);
}

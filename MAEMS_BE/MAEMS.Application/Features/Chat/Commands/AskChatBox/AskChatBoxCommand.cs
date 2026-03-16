using MAEMS.Application.DTOs.Chat;
using MediatR;

namespace MAEMS.Application.Features.Chat.Commands.AskChatBox;

/// <summary>
/// Command để hỏi chatbox
/// </summary>
public class AskChatBoxCommand : IRequest<ChatBoxResponseDto>
{
    /// <summary>
    /// ID của thí sinh
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Câu hỏi
    /// </summary>
    public required string Question { get; set; }
}

/// <summary>
/// Handler cho AskChatBoxCommand
/// </summary>
public class AskChatBoxCommandHandler : IRequestHandler<AskChatBoxCommand, ChatBoxResponseDto>
{
    public async Task<ChatBoxResponseDto> Handle(
        AskChatBoxCommand request,
        CancellationToken cancellationToken)
    {
        // Note: Application layer không trực tiếp gọi Agent
        // Handler này sẽ được gọi từ API Controller
        // Controller sẽ inject ChatBoxAgent và truyền response vào DTO

        // For now, return response template - thực tế sẽ được xử lý ở Controller
        await Task.CompletedTask;

        return new ChatBoxResponseDto
        {
            Question = request.Question,
            Answer = "Response will be provided by the ChatBox Agent",
            CreatedAt = DateTime.UtcNow
        };
    }
}

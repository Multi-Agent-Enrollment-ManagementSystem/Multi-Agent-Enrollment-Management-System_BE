using AutoMapper;
using MAEMS.Application.DTOs.Chat;
using MediatR;

namespace MAEMS.Application.Features.Chat.Queries.GetChatHistory;

/// <summary>
/// Query để lấy lịch sử chat
/// </summary>
public class GetChatHistoryQuery : IRequest<ChatHistoryResponseDto>
{
    /// <summary>
    /// ID của thí sinh
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Số trang (default 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Số messages mỗi trang (default 20)
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Handler cho GetChatHistoryQuery
/// </summary>
public class GetChatHistoryQueryHandler : IRequestHandler<GetChatHistoryQuery, ChatHistoryResponseDto>
{
    private readonly IMapper _mapper;

    public GetChatHistoryQueryHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<ChatHistoryResponseDto> Handle(
        GetChatHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // Note: Application layer không trực tiếp gọi Repository
        // Handler này sẽ được gọi từ API Controller
        // Controller sẽ inject Repository và truyền data vào DTO

        // For now, return empty - thực tế sẽ được xử lý ở Controller
        return new ChatHistoryResponseDto
        {
            Messages = new List<ChatMessageDto>(),
            TotalCount = 0,
            CurrentPage = request.PageNumber,
            TotalPages = 0,
            PageSize = request.PageSize
        };
    }
}

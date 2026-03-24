using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.LlmChatLog;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.LlmChatLogs.Queries.GetAllLlmChatLogs;

public record GetAllLlmChatLogsQuery(
    int? UserId = null,
    string? UserQuery = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<LlmChatLogDto>>>;

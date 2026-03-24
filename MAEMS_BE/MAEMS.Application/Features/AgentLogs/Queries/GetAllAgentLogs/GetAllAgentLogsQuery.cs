using MAEMS.Application.DTOs.AgentLog;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AgentLogs.Queries.GetAllAgentLogs;

public record GetAllAgentLogsQuery(
    int? ApplicationId = null,
    int? DocumentId = null,
    string? AgentType = null,
    string? Status = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<BaseResponse<PagedResponse<AgentLogDto>>>;

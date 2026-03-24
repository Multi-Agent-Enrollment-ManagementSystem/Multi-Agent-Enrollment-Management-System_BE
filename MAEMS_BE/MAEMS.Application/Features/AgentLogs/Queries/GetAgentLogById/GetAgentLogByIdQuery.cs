using MAEMS.Application.DTOs.AgentLog;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AgentLogs.Queries.GetAgentLogById;

public record GetAgentLogByIdQuery(int Id) : IRequest<BaseResponse<AgentLogDto>>;

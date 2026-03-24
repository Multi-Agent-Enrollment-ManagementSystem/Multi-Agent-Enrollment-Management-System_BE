using MAEMS.Application.DTOs.LlmChatLog;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.LlmChatLogs.Queries.GetLlmChatLogById;

public record GetLlmChatLogByIdQuery(int Id) : IRequest<BaseResponse<LlmChatLogDto>>;

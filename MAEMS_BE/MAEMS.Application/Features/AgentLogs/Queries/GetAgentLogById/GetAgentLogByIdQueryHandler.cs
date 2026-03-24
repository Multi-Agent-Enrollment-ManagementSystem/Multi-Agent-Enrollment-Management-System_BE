using AutoMapper;
using MAEMS.Application.DTOs.AgentLog;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AgentLogs.Queries.GetAgentLogById;

public class GetAgentLogByIdQueryHandler : IRequestHandler<GetAgentLogByIdQuery, BaseResponse<AgentLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAgentLogByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<AgentLogDto>> Handle(GetAgentLogByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var agentLog = await _unitOfWork.AgentLogs.GetByIdAsync(request.Id);

            if (agentLog == null)
            {
                return BaseResponse<AgentLogDto>.FailureResponse(
                    "Agent log not found",
                    new List<string> { $"No agent log found with ID: {request.Id}" }
                );
            }

            var agentLogDto = _mapper.Map<AgentLogDto>(agentLog);

            return BaseResponse<AgentLogDto>.SuccessResponse(
                agentLogDto,
                "Agent log retrieved successfully.");
        }
        catch (Exception ex)
        {
            return BaseResponse<AgentLogDto>.FailureResponse(
                "Error retrieving agent log",
                new List<string> { ex.Message }
            );
        }
    }
}

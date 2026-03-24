using AutoMapper;
using MAEMS.Application.DTOs.LlmChatLog;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.LlmChatLogs.Queries.GetLlmChatLogById;

public class GetLlmChatLogByIdQueryHandler : IRequestHandler<GetLlmChatLogByIdQuery, BaseResponse<LlmChatLogDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetLlmChatLogByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<LlmChatLogDto>> Handle(GetLlmChatLogByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var llmChatLog = await _unitOfWork.LlmChatLogs.GetByIdAsync(request.Id);

            if (llmChatLog == null)
            {
                return BaseResponse<LlmChatLogDto>.FailureResponse(
                    "LLM chat log not found",
                    new List<string> { $"No LLM chat log found with ID: {request.Id}" }
                );
            }

            var llmChatLogDto = _mapper.Map<LlmChatLogDto>(llmChatLog);

            return BaseResponse<LlmChatLogDto>.SuccessResponse(
                llmChatLogDto,
                "LLM chat log retrieved successfully.");
        }
        catch (Exception ex)
        {
            return BaseResponse<LlmChatLogDto>.FailureResponse(
                "Error retrieving LLM chat log",
                new List<string> { ex.Message }
            );
        }
    }
}

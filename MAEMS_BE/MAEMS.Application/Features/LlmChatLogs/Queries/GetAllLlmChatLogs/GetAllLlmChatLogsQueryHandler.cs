using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.LlmChatLog;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.LlmChatLogs.Queries.GetAllLlmChatLogs;

public class GetAllLlmChatLogsQueryHandler : IRequestHandler<GetAllLlmChatLogsQuery, BaseResponse<PagedResponse<LlmChatLogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllLlmChatLogsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<LlmChatLogDto>>> Handle(GetAllLlmChatLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.LlmChatLogs.GetLlmChatLogsPagedAsync(
                request.UserId,
                request.UserQuery,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = _mapper.Map<List<LlmChatLogDto>>(items);

            var paged = new PagedResponse<LlmChatLogDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<LlmChatLogDto>>.SuccessResponse(
                paged,
                $"LLM chat logs retrieved successfully. Found {totalCount} chat log(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<LlmChatLogDto>>.FailureResponse(
                "Error retrieving LLM chat logs",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.AgentLog;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.AgentLogs.Queries.GetAllAgentLogs;

public class GetAllAgentLogsQueryHandler : IRequestHandler<GetAllAgentLogsQuery, BaseResponse<PagedResponse<AgentLogDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllAgentLogsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<AgentLogDto>>> Handle(GetAllAgentLogsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (rows, totalCount) = await _unitOfWork.AgentLogs.GetAgentLogsPagedWithApplicantIdAsync(
                request.ApplicationId,
                request.DocumentId,
                request.AgentType,
                request.Status,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = rows
                .Select(x =>
                {
                    var dto = _mapper.Map<AgentLogDto>(x.Log);
                    dto.ApplicantId = x.ApplicantId;
                    return dto;
                })
                .ToList();

            var paged = new PagedResponse<AgentLogDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<AgentLogDto>>.SuccessResponse(
                paged,
                $"Agent logs retrieved successfully. Found {totalCount} agent log(s)."
            );
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<AgentLogDto>>.FailureResponse(
                "Error retrieving agent logs",
                new List<string> { ex.Message }
            );
        }
    }
}

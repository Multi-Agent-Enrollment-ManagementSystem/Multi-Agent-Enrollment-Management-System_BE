using AutoMapper;
using MAEMS.Application.DTOs.Score;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MAEMS.Application.Features.Scores.Queries.GetScoreByApplicantId;

public class GetScoreByApplicantIdQueryHandler : IRequestHandler<GetScoreByApplicantIdQuery, BaseResponse<ScoreDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetScoreByApplicantIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ScoreDto>> Handle(GetScoreByApplicantIdQuery request, CancellationToken cancellationToken)
    {
        var score = await _unitOfWork.Scores.GetByApplicantIdAsync(request.ApplicantId);

        if (score == null)
        {
            return BaseResponse<ScoreDto>.SuccessResponse(
                new ScoreDto { ApplicantId = request.ApplicantId },
                "Score not found but returning empty object"
            );
        }

        var scoreDto = _mapper.Map<ScoreDto>(score);

        return BaseResponse<ScoreDto>.SuccessResponse(scoreDto, "Score retrieved successfully");
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Score;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MAEMS.Application.Features.Scores.Commands.UpdateScore;

public class UpdateScoreCommandHandler : IRequestHandler<UpdateScoreCommand, BaseResponse<ScoreDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateScoreCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ScoreDto>> Handle(UpdateScoreCommand request, CancellationToken cancellationToken)
    {
        var existingScore = await _unitOfWork.Scores.GetByApplicantIdAsync(request.ApplicantId);

        if (existingScore != null)
        {
            // Update existing score
            existingScore.Hk2Math = request.Hk2Math;
            existingScore.Hk2Literature = request.Hk2Literature;
            existingScore.Hk2ForeignLanguage = request.Hk2ForeignLanguage;
            existingScore.Hk2History = request.Hk2History;
            existingScore.Hk2Geography = request.Hk2Geography;
            existingScore.Hk2Physics = request.Hk2Physics;
            existingScore.Hk2Chemistry = request.Hk2Chemistry;
            existingScore.Hk2Biology = request.Hk2Biology;
            existingScore.Hk2EconomicsLaw = request.Hk2EconomicsLaw;
            existingScore.Hk2Informatics = request.Hk2Informatics;
            existingScore.Hk2Technology = request.Hk2Technology;
            
            existingScore.ThptMath = request.ThptMath;
            existingScore.ThptLiterature = request.ThptLiterature;
            existingScore.ThptForeignLanguage = request.ThptForeignLanguage;
            existingScore.ThptHistory = request.ThptHistory;
            existingScore.ThptGeography = request.ThptGeography;
            existingScore.ThptPhysics = request.ThptPhysics;
            existingScore.ThptChemistry = request.ThptChemistry;
            existingScore.ThptBiology = request.ThptBiology;
            existingScore.ThptEconomicsLaw = request.ThptEconomicsLaw;
            existingScore.ThptInformatics = request.ThptInformatics;
            existingScore.ThptTechnology = request.ThptTechnology;
            existingScore.Dgnl = request.Dgnl;

            await _unitOfWork.Scores.UpdateAsync(existingScore);
        }
        else
        {
            // Create new score if it doesn't exist
            existingScore = new Score
            {
                ApplicantId = request.ApplicantId,
                Hk2Math = request.Hk2Math,
                Hk2Literature = request.Hk2Literature,
                Hk2ForeignLanguage = request.Hk2ForeignLanguage,
                Hk2History = request.Hk2History,
                Hk2Geography = request.Hk2Geography,
                Hk2Physics = request.Hk2Physics,
                Hk2Chemistry = request.Hk2Chemistry,
                Hk2Biology = request.Hk2Biology,
                Hk2EconomicsLaw = request.Hk2EconomicsLaw,
                Hk2Informatics = request.Hk2Informatics,
                Hk2Technology = request.Hk2Technology,
                
                ThptMath = request.ThptMath,
                ThptLiterature = request.ThptLiterature,
                ThptForeignLanguage = request.ThptForeignLanguage,
                ThptHistory = request.ThptHistory,
                ThptGeography = request.ThptGeography,
                ThptPhysics = request.ThptPhysics,
                ThptChemistry = request.ThptChemistry,
                ThptBiology = request.ThptBiology,
                ThptEconomicsLaw = request.ThptEconomicsLaw,
                ThptInformatics = request.ThptInformatics,
                ThptTechnology = request.ThptTechnology,
                Dgnl = request.Dgnl
            };

            await _unitOfWork.Scores.AddAsync(existingScore);
        }
        
        await _unitOfWork.SaveChangesAsync();

        var scoreDto = _mapper.Map<ScoreDto>(existingScore);

        return BaseResponse<ScoreDto>.SuccessResponse(scoreDto, "Score updated successfully");
    }
}

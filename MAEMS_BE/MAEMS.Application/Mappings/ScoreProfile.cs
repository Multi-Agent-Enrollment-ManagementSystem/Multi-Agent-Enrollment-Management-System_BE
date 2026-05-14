using AutoMapper;
using MAEMS.Application.DTOs.Score;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class ScoreProfile : Profile
{
    public ScoreProfile()
    {
        CreateMap<Score, ScoreDto>().ReverseMap();
    }
}

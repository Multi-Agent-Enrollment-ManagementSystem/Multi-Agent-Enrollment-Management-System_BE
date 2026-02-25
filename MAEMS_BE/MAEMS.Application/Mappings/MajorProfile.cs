using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class MajorProfile : Profile
{
    public MajorProfile()
    {
        CreateMap<Major, MajorDto>().ReverseMap();
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class CampusProfile : Profile
{
    public CampusProfile()
    {
        CreateMap<Campus, CampusDto>().ReverseMap();
        
        CreateMap<Campus, CampusBasicDto>();
    }
}

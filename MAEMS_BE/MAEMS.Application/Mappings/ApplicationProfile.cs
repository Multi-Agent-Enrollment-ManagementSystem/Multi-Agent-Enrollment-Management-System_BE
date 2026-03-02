using AutoMapper;
using MAEMS.Application.DTOs.Application;
using DomainApplication = MAEMS.Domain.Entities.Application;

namespace MAEMS.Application.Mappings;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<DomainApplication, ApplicationDto>().ReverseMap();
        CreateMap<DomainApplication, ApplicationBasicDto>();
        CreateMap<CreateApplicationRequestDto, DomainApplication>();
        CreateMap<UpdateApplicationRequestDto, DomainApplication>();
    }
}
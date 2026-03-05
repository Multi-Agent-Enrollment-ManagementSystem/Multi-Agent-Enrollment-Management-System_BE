using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Entities;
using DomainApplication = MAEMS.Domain.Entities.Application;
namespace MAEMS.Application.Mappings;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<MAEMS.Domain.Entities.Application, ApplicationDto>();
        CreateMap<DomainApplication, ApplicationDto>().ReverseMap();
        CreateMap<DomainApplication, ApplicationBasicDto>();
        CreateMap<CreateApplicationRequestDto, DomainApplication>();
        CreateMap<UpdateApplicationRequestDto, DomainApplication>();
        CreateMap<MAEMS.Domain.Entities.Application, MyApplicationDto>();
        CreateMap<MAEMS.Domain.Entities.Application, FullApplicationDto>();
        CreateMap<MAEMS.Domain.Entities.Application, ApplicationWithDocumentsDto>();
    }
}
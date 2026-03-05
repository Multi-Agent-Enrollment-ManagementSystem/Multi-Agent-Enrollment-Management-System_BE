using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class ApplicationProfile : Profile
{
    public ApplicationProfile()
    {
        CreateMap<MAEMS.Domain.Entities.Application, MyApplicationDto>();
        CreateMap<MAEMS.Domain.Entities.Application, FullApplicationDto>();
    }
}
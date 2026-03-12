using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using DomainConfig = MAEMS.Domain.Entities.ProgramAdmissionConfig;

namespace MAEMS.Application.Mappings;

public class ProgramAdmissionConfigProfile : Profile
{
    public ProgramAdmissionConfigProfile()
    {
        CreateMap<DomainConfig, ProgramAdmissionConfigDto>();

        // DomainConfig → ProgramCampusAdmissionDto (admission info per config row)
        CreateMap<DomainConfig, ProgramCampusAdmissionDto>()
            .ForMember(dest => dest.ConfigId, opt => opt.MapFrom(src => src.ConfigId))
            .ForMember(dest => dest.AdmissionTypeId, opt => opt.MapFrom(src => src.AdmissionTypeId))
            .ForMember(dest => dest.AdmissionTypeName, opt => opt.MapFrom(src => src.AdmissionTypeName))
            .ForMember(dest => dest.Quota, opt => opt.MapFrom(src => src.Quota))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
    }
}

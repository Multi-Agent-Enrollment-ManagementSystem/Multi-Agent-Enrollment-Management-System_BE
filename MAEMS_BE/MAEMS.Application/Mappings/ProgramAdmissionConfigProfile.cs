using AutoMapper;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using DomainConfig = MAEMS.Domain.Entities.ProgramAdmissionConfig;

namespace MAEMS.Application.Mappings;

public class ProgramAdmissionConfigProfile : Profile
{
    public ProgramAdmissionConfigProfile()
    {
        CreateMap<DomainConfig, ProgramAdmissionConfigDto>();
    }
}

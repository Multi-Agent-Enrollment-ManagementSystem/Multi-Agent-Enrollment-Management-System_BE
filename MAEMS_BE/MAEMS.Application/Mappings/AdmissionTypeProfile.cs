using AutoMapper;
using MAEMS.Application.DTOs.AdmissionType;
using DomainAdmissionType = MAEMS.Domain.Entities.AdmissionType;

namespace MAEMS.Application.Mappings;

public class AdmissionTypeProfile : Profile
{
    public AdmissionTypeProfile()
    {
        CreateMap<DomainAdmissionType, AdmissionTypeBasicDto>();
        CreateMap<DomainAdmissionType, AdmissionTypeDto>();
    }
}

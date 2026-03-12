using AutoMapper;
using MAEMS.Application.DTOs.Program;
using DomainProgram = MAEMS.Domain.Entities.Program;

namespace MAEMS.Application.Mappings;

public class ProgramProfile : Profile
{
    public ProgramProfile()
    {
        CreateMap<DomainProgram, ProgramDto>()
            .ForMember(dest => dest.MajorName, opt => opt.Ignore())
            .ForMember(dest => dest.Campuses, opt => opt.Ignore());

        CreateMap<DomainProgram, ProgramBasicDto>()
            .ForMember(dest => dest.MajorName, opt => opt.Ignore());
    }
}

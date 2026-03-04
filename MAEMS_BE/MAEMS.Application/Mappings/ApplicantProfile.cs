using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class ApplicantProfile : Profile
{
    public ApplicantProfile()
    {
        CreateMap<Applicant, ApplicantDto>().ReverseMap();
        CreateMap<CreateApplicantRequestDto, Applicant>();
        CreateMap<UpdateApplicantRequestDto, Applicant>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
    }
}

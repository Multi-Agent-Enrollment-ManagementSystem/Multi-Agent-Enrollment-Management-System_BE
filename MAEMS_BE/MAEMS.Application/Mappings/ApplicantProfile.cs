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
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class EnrollmentYearProfile : Profile
{
    public EnrollmentYearProfile()
    {
        CreateMap<EnrollmentYear, EnrollmentYearDto>().ReverseMap();
    }
}

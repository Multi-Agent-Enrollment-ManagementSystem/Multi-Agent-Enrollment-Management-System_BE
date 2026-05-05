using AutoMapper;
using MAEMS.Application.DTOs.TuitionFee;
using DomainTuitionFee = MAEMS.Domain.Entities.TuitionFee;

namespace MAEMS.Application.Mappings;

public class TuitionFeeProfile : Profile
{
    public TuitionFeeProfile()
    {
        CreateMap<DomainTuitionFee, TuitionFeeDto>();
        CreateMap<CreateTuitionFeeRequest, DomainTuitionFee>();
        CreateMap<UpdateTuitionFeeRequest, DomainTuitionFee>();
    }
}

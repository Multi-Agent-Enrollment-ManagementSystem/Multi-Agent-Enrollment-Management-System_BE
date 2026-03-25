using AutoMapper;
using MAEMS.Application.DTOs.Payment;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class PaymentProfile : Profile
{
    public PaymentProfile()
    {
        CreateMap<Payment, SubmitApplicationPaymentDto>()
            .ForMember(d => d.Url, opt => opt.Ignore());

        CreateMap<Payment, PaymentDto>();
    }
}

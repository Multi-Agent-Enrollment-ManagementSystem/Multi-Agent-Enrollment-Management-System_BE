using AutoMapper;
using MAEMS.Application.DTOs.Role;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class RoleProfile : Profile
{
    public RoleProfile()
    {
        CreateMap<Role, RoleDto>().ReverseMap();
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.User;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<User, UserDetailDto>();

        // Mapping for Login response - only essential fields
        CreateMap<User, LoginUserDto>()
            .ForMember(dest => dest.Role, opt => opt.Ignore()); // Role will be set manually after fetching from database
        
        // Mapping for User Profile
        CreateMap<User, UserProfileDto>()
            .ForMember(dest => dest.RoleName, opt => opt.Ignore()); // RoleName will be set manually after fetching from database
    }
}

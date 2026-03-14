using MAEMS.Application.DTOs.Applicant;

namespace MAEMS.Application.DTOs.User;

public class UserDetailDto : UserDto
{
    public ApplicantDto? Applicant { get; set; }
}

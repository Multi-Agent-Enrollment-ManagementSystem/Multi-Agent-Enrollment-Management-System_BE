using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Commands.CreateApplicant;

public class CreateApplicantCommandHandler : IRequestHandler<CreateApplicantCommand, BaseResponse<ApplicantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateApplicantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicantDto>> Handle(CreateApplicantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Kiểm tra xem userId đã có applicant chưa
            var existingApplicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
            if (existingApplicant != null)
            {
                return BaseResponse<ApplicantDto>.FailureResponse(
                    "Applicant already exists",
                    new List<string> { "This user already has an applicant profile" }
                );
            }

            // Tạo applicant mới
            var applicant = new Applicant
            {
                UserId = request.UserId,
                FullName = request.FullName,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                HighSchoolName = request.HighSchoolName,
                HighSchoolDistrict = request.HighSchoolDistrict,
                HighSchoolProvince = request.HighSchoolProvince,
                GraduationYear = request.GraduationYear,
                IdIssueNumber = request.IdIssueNumber,
                IdIssueDate = request.IdIssueDate,
                IdIssuePlace = request.IdIssuePlace,
                ContactName = request.ContactName,
                ContactAddress = request.ContactAddress,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                AllowShare = request.AllowShare
            };

            var createdApplicant = await _unitOfWork.Applicants.AddAsync(applicant);
            await _unitOfWork.SaveChangesAsync();

            var applicantDto = _mapper.Map<ApplicantDto>(createdApplicant);
            
            return BaseResponse<ApplicantDto>.SuccessResponse(applicantDto, "Applicant created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicantDto>.FailureResponse(
                "Error creating applicant",
                new List<string> { ex.Message }
            );
        }
    }
}

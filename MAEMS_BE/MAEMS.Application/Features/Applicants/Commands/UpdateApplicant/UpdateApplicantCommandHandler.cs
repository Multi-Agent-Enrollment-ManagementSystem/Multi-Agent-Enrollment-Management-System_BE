using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Commands.UpdateApplicant;

public class UpdateApplicantCommandHandler : IRequestHandler<UpdateApplicantCommand, BaseResponse<ApplicantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateApplicantCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicantDto>> Handle(UpdateApplicantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Tìm applicant theo userId
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
            if (applicant == null)
            {
                return BaseResponse<ApplicantDto>.FailureResponse(
                    "Applicant not found",
                    new List<string> { "No applicant profile found for this user" }
                );
            }

            // Chỉ cập nhật các field được truyền vào (PATCH - partial update)
            if (request.FullName != null)
                applicant.FullName = request.FullName;

            if (request.DateOfBirth.HasValue)
                applicant.DateOfBirth = request.DateOfBirth;

            if (request.Gender != null)
                applicant.Gender = request.Gender;

            if (request.HighSchoolName != null)
                applicant.HighSchoolName = request.HighSchoolName;

            if (request.HighSchoolDistrict != null)
                applicant.HighSchoolDistrict = request.HighSchoolDistrict;

            if (request.HighSchoolProvince != null)
                applicant.HighSchoolProvince = request.HighSchoolProvince;

            if (request.GraduationYear.HasValue)
                applicant.GraduationYear = request.GraduationYear;

            if (request.IdIssueNumber != null)
                applicant.IdIssueNumber = request.IdIssueNumber;

            if (request.IdIssueDate.HasValue)
                applicant.IdIssueDate = request.IdIssueDate;

            if (request.IdIssuePlace != null)
                applicant.IdIssuePlace = request.IdIssuePlace;

            if (request.ContactName != null)
                applicant.ContactName = request.ContactName;

            if (request.ContactAddress != null)
                applicant.ContactAddress = request.ContactAddress;

            if (request.ContactPhone != null)
                applicant.ContactPhone = request.ContactPhone;

            if (request.ContactEmail != null)
                applicant.ContactEmail = request.ContactEmail;

            if (request.AllowShare.HasValue)
                applicant.AllowShare = request.AllowShare;

            await _unitOfWork.Applicants.UpdateAsync(applicant);
            await _unitOfWork.SaveChangesAsync();

            var applicantDto = _mapper.Map<ApplicantDto>(applicant);

            return BaseResponse<ApplicantDto>.SuccessResponse(applicantDto, "Applicant updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicantDto>.FailureResponse(
                "Error updating applicant",
                new List<string> { ex.Message }
            );
        }
    }
}

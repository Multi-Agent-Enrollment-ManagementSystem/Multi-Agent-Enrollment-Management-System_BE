using AutoMapper;
using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.CreateEnrollmentYear;

public class CreateEnrollmentYearCommandHandler : IRequestHandler<CreateEnrollmentYearCommand, BaseResponse<EnrollmentYearDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateEnrollmentYearCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EnrollmentYearDto>> Handle(CreateEnrollmentYearCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Year))
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    "Invalid enrollment year",
                    new List<string> { "Year is required" }
                );
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    "Invalid enrollment year",
                    new List<string> { "Status is required" }
                );
            }

            if (request.RegistrationStartDate.HasValue && request.RegistrationEndDate.HasValue &&
                request.RegistrationStartDate.Value > request.RegistrationEndDate.Value)
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    "Invalid enrollment year",
                    new List<string> { "RegistrationStartDate must be before or equal to RegistrationEndDate" }
                );
            }

            var yearText = request.Year.Trim();

            // Optional: avoid duplicate year
            var exists = await _unitOfWork.EnrollmentYears.ExistsAsync(y => y.Year.ToLower() == yearText.ToLower());
            if (exists)
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    "Enrollment year already exists",
                    new List<string> { "An enrollment year with the same Year already exists" }
                );
            }

            var entity = new EnrollmentYear
            {
                Year = yearText,
                RegistrationStartDate = request.RegistrationStartDate,
                RegistrationEndDate = request.RegistrationEndDate,
                Status = request.Status.Trim(),
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var created = await _unitOfWork.EnrollmentYears.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<EnrollmentYearDto>(created);
            return BaseResponse<EnrollmentYearDto>.SuccessResponse(dto, "Enrollment year created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<EnrollmentYearDto>.FailureResponse(
                "Error creating enrollment year",
                new List<string> { ex.Message }
            );
        }
    }
}

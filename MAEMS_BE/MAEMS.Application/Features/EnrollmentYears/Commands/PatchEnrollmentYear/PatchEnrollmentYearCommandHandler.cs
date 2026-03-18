using AutoMapper;
using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.PatchEnrollmentYear;

public class PatchEnrollmentYearCommandHandler : IRequestHandler<PatchEnrollmentYearCommand, BaseResponse<EnrollmentYearDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchEnrollmentYearCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<EnrollmentYearDto>> Handle(PatchEnrollmentYearCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _unitOfWork.EnrollmentYears.GetByIdAsync(request.EnrollmentYearId);
            if (entity == null)
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    $"EnrollmentYear with ID {request.EnrollmentYearId} not found",
                    new List<string> { "EnrollmentYear not found" }
                );
            }

            // apply incoming changes
            if (request.Year != null)
            {
                if (string.IsNullOrWhiteSpace(request.Year))
                {
                    return BaseResponse<EnrollmentYearDto>.FailureResponse(
                        "Invalid enrollment year",
                        new List<string> { "Year cannot be empty" }
                    );
                }

                var yearText = request.Year.Trim();
                var exists = await _unitOfWork.EnrollmentYears.ExistsAsync(y => y.EnrollmentYearId != request.EnrollmentYearId && y.Year.ToLower() == yearText.ToLower());
                if (exists)
                {
                    return BaseResponse<EnrollmentYearDto>.FailureResponse(
                        "Enrollment year already exists",
                        new List<string> { "An enrollment year with the same Year already exists" }
                    );
                }

                entity.Year = yearText;
            }

            if (request.Status != null)
            {
                if (string.IsNullOrWhiteSpace(request.Status))
                {
                    return BaseResponse<EnrollmentYearDto>.FailureResponse(
                        "Invalid enrollment year",
                        new List<string> { "Status cannot be empty" }
                    );
                }

                entity.Status = request.Status.Trim();
            }

            if (request.RegistrationStartDate.HasValue)
                entity.RegistrationStartDate = request.RegistrationStartDate;

            if (request.RegistrationEndDate.HasValue)
                entity.RegistrationEndDate = request.RegistrationEndDate;

            if (entity.RegistrationStartDate.HasValue && entity.RegistrationEndDate.HasValue &&
                entity.RegistrationStartDate.Value > entity.RegistrationEndDate.Value)
            {
                return BaseResponse<EnrollmentYearDto>.FailureResponse(
                    "Invalid enrollment year",
                    new List<string> { "RegistrationStartDate must be before or equal to RegistrationEndDate" }
                );
            }

            await _unitOfWork.EnrollmentYears.UpdateAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<EnrollmentYearDto>(entity);
            return BaseResponse<EnrollmentYearDto>.SuccessResponse(dto, "Enrollment year updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<EnrollmentYearDto>.FailureResponse(
                "Error updating enrollment year",
                new List<string> { ex.Message }
            );
        }
    }
}

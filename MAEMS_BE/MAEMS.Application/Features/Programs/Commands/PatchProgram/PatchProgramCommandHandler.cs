using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Commands.PatchProgram;

public class PatchProgramCommandHandler : IRequestHandler<PatchProgramCommand, BaseResponse<ProgramDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchProgramCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramDto>> Handle(PatchProgramCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(request.ProgramId);
            if (program == null)
            {
                return BaseResponse<ProgramDto>.FailureResponse(
                    $"Program with ID {request.ProgramId} not found",
                    new List<string> { "Program not found" }
                );
            }

            // Partial update: only update provided fields
            if (request.ProgramName != null)
            {
                if (string.IsNullOrWhiteSpace(request.ProgramName))
                {
                    return BaseResponse<ProgramDto>.FailureResponse(
                        "Invalid program",
                        new List<string> { "ProgramName cannot be empty" }
                    );
                }
                program.ProgramName = request.ProgramName.Trim();
            }

            if (request.Description != null)
                program.Description = request.Description;

            if (request.CareerProspects != null)
                program.CareerProspects = request.CareerProspects;

            if (request.Duration != null)
                program.Duration = request.Duration;

            if (request.IsActive.HasValue)
                program.IsActive = request.IsActive;

            await _unitOfWork.Programs.UpdateAsync(program);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<ProgramDto>(program);

            if (program.MajorId.HasValue)
            {
                var major = await _unitOfWork.Majors.GetByIdAsync(program.MajorId.Value);
                dto.MajorName = major?.MajorName ?? string.Empty;
            }
            else
            {
                dto.MajorName = string.Empty;
            }

            // EnrollmentYear text is filled by query handlers. Keep current value if available.
            dto.EnrollmentYear = program.EnrollmentYear;
            dto.Campuses = new();

            return BaseResponse<ProgramDto>.SuccessResponse(dto, "Program updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramDto>.FailureResponse(
                "Error updating program",
                new List<string> { ex.Message }
            );
        }
    }
}

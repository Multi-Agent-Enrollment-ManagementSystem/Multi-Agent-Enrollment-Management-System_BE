using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Commands.CreateProgram;

public class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, BaseResponse<ProgramDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateProgramCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramDto>> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ProgramName))
            {
                return BaseResponse<ProgramDto>.FailureResponse(
                    "Invalid program",
                    new List<string> { "ProgramName is required" }
                );
            }

            // Validate MajorId if provided
            if (request.MajorId.HasValue)
            {
                var major = await _unitOfWork.Majors.GetByIdAsync(request.MajorId.Value);
                if (major == null)
                {
                    return BaseResponse<ProgramDto>.FailureResponse(
                        "Invalid major",
                        new List<string> { "Major not found" }
                    );
                }
            }

            // Validate EnrollmentYearId if provided
            // NOTE: EnrollmentYears repository is not exposed in IUnitOfWork in this solution.
            // We rely on DB foreign key constraints (and/or later queries) instead of checking here.

            // Optional: avoid duplicate name within same major/year
            var exists = await _unitOfWork.Programs.ExistsAsync(p =>
                p.ProgramName.ToLower() == request.ProgramName.Trim().ToLower() &&
                p.MajorId == request.MajorId &&
                p.EnrollmentYearId == request.EnrollmentYearId);

            if (exists)
            {
                return BaseResponse<ProgramDto>.FailureResponse(
                    "Program already exists",
                    new List<string> { "A program with the same name already exists" }
                );
            }

            var program = new Program
            {
                ProgramName = request.ProgramName.Trim(),
                MajorId = request.MajorId,
                EnrollmentYearId = request.EnrollmentYearId,
                Description = request.Description,
                CareerProspects = request.CareerProspects,
                Duration = request.Duration,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var created = await _unitOfWork.Programs.AddAsync(program);
            await _unitOfWork.SaveChangesAsync();

            // Reload with navigation-derived fields handled by queries; map basic fields here.
            var dto = _mapper.Map<ProgramDto>(created);

            if (created.MajorId.HasValue)
            {
                var major = await _unitOfWork.Majors.GetByIdAsync(created.MajorId.Value);
                dto.MajorName = major?.MajorName ?? string.Empty;
            }
            else
            {
                dto.MajorName = string.Empty;
            }

            // EnrollmentYear text is filled by query handlers that join EnrollmentYear.
            dto.EnrollmentYear = null;

            dto.Campuses = new();

            return BaseResponse<ProgramDto>.SuccessResponse(dto, "Program created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramDto>.FailureResponse(
                "Error creating program",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Commands.PatchMajor;

public class PatchMajorCommandHandler : IRequestHandler<PatchMajorCommand, BaseResponse<MajorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchMajorCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<MajorDto>> Handle(PatchMajorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var major = await _unitOfWork.Majors.GetByIdAsync(request.MajorId);
            if (major == null)
            {
                return BaseResponse<MajorDto>.FailureResponse(
                    $"Major with ID {request.MajorId} not found",
                    new List<string> { "Major not found" }
                );
            }

            if (request.MajorCode != null)
            {
                if (string.IsNullOrWhiteSpace(request.MajorCode))
                {
                    return BaseResponse<MajorDto>.FailureResponse(
                        "Invalid major",
                        new List<string> { "MajorCode cannot be empty" }
                    );
                }

                var code = request.MajorCode.Trim();
                var exists = await _unitOfWork.Majors.ExistsAsync(m => m.MajorId != request.MajorId && m.MajorCode.ToLower() == code.ToLower());
                if (exists)
                {
                    return BaseResponse<MajorDto>.FailureResponse(
                        "Major already exists",
                        new List<string> { "A major with the same code already exists" }
                    );
                }

                major.MajorCode = code;
            }

            if (request.MajorName != null)
            {
                if (string.IsNullOrWhiteSpace(request.MajorName))
                {
                    return BaseResponse<MajorDto>.FailureResponse(
                        "Invalid major",
                        new List<string> { "MajorName cannot be empty" }
                    );
                }

                major.MajorName = request.MajorName.Trim();
            }

            if (request.Description != null)
                major.Description = request.Description;

            if (request.IsActive.HasValue)
                major.IsActive = request.IsActive;

            await _unitOfWork.Majors.UpdateAsync(major);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<MajorDto>(major);
            return BaseResponse<MajorDto>.SuccessResponse(dto, "Major updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<MajorDto>.FailureResponse(
                "Error updating major",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Commands.CreateMajor;

public class CreateMajorCommandHandler : IRequestHandler<CreateMajorCommand, BaseResponse<MajorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateMajorCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<MajorDto>> Handle(CreateMajorCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.MajorCode))
            {
                return BaseResponse<MajorDto>.FailureResponse(
                    "Invalid major",
                    new List<string> { "MajorCode is required" }
                );
            }

            if (string.IsNullOrWhiteSpace(request.MajorName))
            {
                return BaseResponse<MajorDto>.FailureResponse(
                    "Invalid major",
                    new List<string> { "MajorName is required" }
                );
            }

            // Optional: avoid duplicate MajorCode
            var code = request.MajorCode.Trim();
            var name = request.MajorName.Trim();

            var exists = await _unitOfWork.Majors.ExistsAsync(m => m.MajorCode.ToLower() == code.ToLower());
            if (exists)
            {
                return BaseResponse<MajorDto>.FailureResponse(
                    "Major already exists",
                    new List<string> { "A major with the same code already exists" }
                );
            }

            var major = new Major
            {
                MajorCode = code,
                MajorName = name,
                Description = request.Description,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };

            var created = await _unitOfWork.Majors.AddAsync(major);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<MajorDto>(created);
            return BaseResponse<MajorDto>.SuccessResponse(dto, "Major created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<MajorDto>.FailureResponse(
                "Error creating major",
                new List<string> { ex.Message }
            );
        }
    }
}

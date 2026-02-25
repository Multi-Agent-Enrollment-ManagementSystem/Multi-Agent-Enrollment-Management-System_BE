using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetMajorById;

public class GetMajorByIdQueryHandler : IRequestHandler<GetMajorByIdQuery, BaseResponse<MajorDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMajorByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<MajorDto>> Handle(GetMajorByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var major = await _unitOfWork.Majors.GetByIdAsync(request.Id);

            if (major == null)
            {
                return BaseResponse<MajorDto>.FailureResponse(
                    $"Major with ID {request.Id} not found",
                    new List<string> { "Major not found" }
                );
            }

            var majorDto = _mapper.Map<MajorDto>(major);

            return BaseResponse<MajorDto>.SuccessResponse(majorDto, "Major retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<MajorDto>.FailureResponse(
                "Error retrieving major",
                new List<string> { ex.Message }
            );
        }
    }
}

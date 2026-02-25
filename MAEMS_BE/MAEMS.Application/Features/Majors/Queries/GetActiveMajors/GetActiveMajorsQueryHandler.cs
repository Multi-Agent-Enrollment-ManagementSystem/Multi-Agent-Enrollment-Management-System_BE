using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetActiveMajors;

public class GetActiveMajorsQueryHandler : IRequestHandler<GetActiveMajorsQuery, BaseResponse<IEnumerable<MajorDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveMajorsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<MajorDto>>> Handle(GetActiveMajorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var majors = await _unitOfWork.Majors.GetActiveMajorsAsync();
            var majorDtos = _mapper.Map<IEnumerable<MajorDto>>(majors);

            return BaseResponse<IEnumerable<MajorDto>>.SuccessResponse(majorDtos, "Active majors retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<MajorDto>>.FailureResponse(
                "Error retrieving active majors", 
                new List<string> { ex.Message }
            );
        }
    }
}

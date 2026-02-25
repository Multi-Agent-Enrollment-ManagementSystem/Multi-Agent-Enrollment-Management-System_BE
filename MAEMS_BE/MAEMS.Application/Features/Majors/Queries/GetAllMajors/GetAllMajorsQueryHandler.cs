using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetAllMajors;

public class GetAllMajorsQueryHandler : IRequestHandler<GetAllMajorsQuery, BaseResponse<IEnumerable<MajorDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllMajorsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<MajorDto>>> Handle(GetAllMajorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var majors = await _unitOfWork.Majors.GetAllAsync();
            var majorDtos = _mapper.Map<IEnumerable<MajorDto>>(majors);

            return BaseResponse<IEnumerable<MajorDto>>.SuccessResponse(majorDtos, "Majors retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<MajorDto>>.FailureResponse(
                "Error retrieving majors", 
                new List<string> { ex.Message }
            );
        }
    }
}

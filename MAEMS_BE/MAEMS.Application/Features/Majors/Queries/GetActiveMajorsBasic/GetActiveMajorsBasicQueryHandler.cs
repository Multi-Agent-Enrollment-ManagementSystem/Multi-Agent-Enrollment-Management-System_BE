using AutoMapper;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetActiveMajorsBasic;

public class GetActiveMajorsBasicQueryHandler : IRequestHandler<GetActiveMajorsBasicQuery, BaseResponse<IEnumerable<MajorBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveMajorsBasicQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<MajorBasicDto>>> Handle(GetActiveMajorsBasicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var majors = await _unitOfWork.Majors.GetActiveMajorsAsync();
            var majorDtos = _mapper.Map<IEnumerable<MajorBasicDto>>(majors);

            return BaseResponse<IEnumerable<MajorBasicDto>>.SuccessResponse(majorDtos, "Active majors retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<MajorBasicDto>>.FailureResponse(
                "Error retrieving active majors",
                new List<string> { ex.Message }
            );
        }
    }
}

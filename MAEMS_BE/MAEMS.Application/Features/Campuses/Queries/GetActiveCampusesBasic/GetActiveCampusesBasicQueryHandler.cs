using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetActiveCampusesBasic;

public class GetActiveCampusesBasicQueryHandler : IRequestHandler<GetActiveCampusesBasicQuery, BaseResponse<IEnumerable<CampusBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveCampusesBasicQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<CampusBasicDto>>> Handle(GetActiveCampusesBasicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var campuses = await _unitOfWork.Campuses.GetActiveCampusesAsync();
            var campusDtos = _mapper.Map<IEnumerable<CampusBasicDto>>(campuses);

            return BaseResponse<IEnumerable<CampusBasicDto>>.SuccessResponse(campusDtos, "Active campuses retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CampusBasicDto>>.FailureResponse(
                "Error retrieving active campuses",
                new List<string> { ex.Message }
            );
        }
    }
}

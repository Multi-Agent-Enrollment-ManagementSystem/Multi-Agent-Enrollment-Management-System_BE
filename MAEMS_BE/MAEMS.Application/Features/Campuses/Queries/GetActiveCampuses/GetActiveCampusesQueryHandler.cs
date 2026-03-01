using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetActiveCampuses;

public class GetActiveCampusesQueryHandler : IRequestHandler<GetActiveCampusesQuery, BaseResponse<IEnumerable<CampusDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveCampusesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<CampusDto>>> Handle(GetActiveCampusesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var campuses = await _unitOfWork.Campuses.GetActiveCampusesAsync();
            var campusDtos = _mapper.Map<IEnumerable<CampusDto>>(campuses);

            return BaseResponse<IEnumerable<CampusDto>>.SuccessResponse(campusDtos, "Active campuses retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CampusDto>>.FailureResponse(
                "Error retrieving active campuses",
                new List<string> { ex.Message }
            );
        }
    }
}

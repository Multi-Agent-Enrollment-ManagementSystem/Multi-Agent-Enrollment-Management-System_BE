using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetAllCampuses;

public class GetAllCampusesQueryHandler : IRequestHandler<GetAllCampusesQuery, BaseResponse<IEnumerable<CampusDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllCampusesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<CampusDto>>> Handle(GetAllCampusesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var campuses = await _unitOfWork.Campuses.GetAllAsync();
            var campusDtos = _mapper.Map<IEnumerable<CampusDto>>(campuses);

            return BaseResponse<IEnumerable<CampusDto>>.SuccessResponse(campusDtos, "Campuses retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<CampusDto>>.FailureResponse(
                "Error retrieving campuses",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Queries.GetCampusById;

public class GetCampusByIdQueryHandler : IRequestHandler<GetCampusByIdQuery, BaseResponse<CampusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetCampusByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CampusDto>> Handle(GetCampusByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var campus = await _unitOfWork.Campuses.GetByIdAsync(request.Id);

            if (campus == null)
            {
                return BaseResponse<CampusDto>.FailureResponse(
                    $"Campus with ID {request.Id} not found",
                    new List<string> { "Campus not found" }
                );
            }

            var campusDto = _mapper.Map<CampusDto>(campus);

            return BaseResponse<CampusDto>.SuccessResponse(campusDto, "Campus retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<CampusDto>.FailureResponse(
                "Error retrieving campus",
                new List<string> { ex.Message }
            );
        }
    }
}

using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Commands.PatchCampus;

public class PatchCampusCommandHandler : IRequestHandler<PatchCampusCommand, BaseResponse<CampusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchCampusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CampusDto>> Handle(PatchCampusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var campus = await _unitOfWork.Campuses.GetByIdAsync(request.CampusId);
            if (campus == null)
            {
                return BaseResponse<CampusDto>.FailureResponse(
                    $"Campus with ID {request.CampusId} not found",
                    new List<string> { "Campus not found" }
                );
            }

            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BaseResponse<CampusDto>.FailureResponse(
                        "Invalid campus",
                        new List<string> { "Name cannot be empty" }
                    );
                }

                var name = request.Name.Trim();
                var exists = await _unitOfWork.Campuses.ExistsAsync(c => c.CampusId != request.CampusId && c.Name.ToLower() == name.ToLower());
                if (exists)
                {
                    return BaseResponse<CampusDto>.FailureResponse(
                        "Campus already exists",
                        new List<string> { "A campus with the same name already exists" }
                    );
                }

                campus.Name = name;
            }

            if (request.Address != null)
                campus.Address = request.Address;

            if (request.Description != null)
                campus.Description = request.Description;

            if (request.IsActive.HasValue)
                campus.IsActive = request.IsActive;

            await _unitOfWork.Campuses.UpdateAsync(campus);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<CampusDto>(campus);
            return BaseResponse<CampusDto>.SuccessResponse(dto, "Campus updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<CampusDto>.FailureResponse(
                "Error updating campus",
                new List<string> { ex.Message }
            );
        }
    }
}

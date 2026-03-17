using AutoMapper;
using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Commands.CreateCampus;

public class CreateCampusCommandHandler : IRequestHandler<CreateCampusCommand, BaseResponse<CampusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCampusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<CampusDto>> Handle(CreateCampusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BaseResponse<CampusDto>.FailureResponse(
                    "Invalid campus",
                    new List<string> { "Name is required" }
                );
            }

            var name = request.Name.Trim();

            // Optional: avoid duplicate campus name
            var exists = await _unitOfWork.Campuses.ExistsAsync(c => c.Name.ToLower() == name.ToLower());
            if (exists)
            {
                return BaseResponse<CampusDto>.FailureResponse(
                    "Campus already exists",
                    new List<string> { "A campus with the same name already exists" }
                );
            }

            var campus = new Campus
            {
                Name = name,
                Address = request.Address,
                Description = request.Description,
                IsActive = request.IsActive ?? true
            };

            var created = await _unitOfWork.Campuses.AddAsync(campus);
            await _unitOfWork.SaveChangesAsync();

            var dto = _mapper.Map<CampusDto>(created);
            return BaseResponse<CampusDto>.SuccessResponse(dto, "Campus created successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<CampusDto>.FailureResponse(
                "Error creating campus",
                new List<string> { ex.Message }
            );
        }
    }
}

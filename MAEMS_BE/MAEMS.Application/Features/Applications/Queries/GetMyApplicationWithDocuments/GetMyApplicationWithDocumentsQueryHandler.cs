using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplicationWithDocuments;

public class GetMyApplicationWithDocumentsQueryHandler : IRequestHandler<GetMyApplicationWithDocumentsQuery, BaseResponse<ApplicationWithDocumentsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicationWithDocumentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicationWithDocumentsDto>> Handle(GetMyApplicationWithDocumentsQuery request, CancellationToken cancellationToken)
    {
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Applicant not found", new() { "No applicant profile found for this user" });
        }

        var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
        if (application == null)
        {
            return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Application not found", new() { $"No application found with ID {request.ApplicationId}" });
        }

        if (application.ApplicantId != applicant.ApplicantId)
        {
            return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Forbidden", new() { "You do not have access to this application" });
        }

        var documents = await _unitOfWork.Documents.GetByApplicationIdAsync(application.ApplicationId);
        var dto = _mapper.Map<ApplicationWithDocumentsDto>(application);
        dto.Documents = _mapper.Map<List<DocumentDto>>(documents);

        return BaseResponse<ApplicationWithDocumentsDto>.SuccessResponse(dto, "Application and documents retrieved successfully");
    }
}
using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetApplicationWithDocuments;

public class GetApplicationWithDocumentsQueryHandler : IRequestHandler<GetApplicationWithDocumentsQuery, BaseResponse<ApplicationWithDocumentsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetApplicationWithDocumentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicationWithDocumentsDto>> Handle(GetApplicationWithDocumentsQuery request, CancellationToken cancellationToken)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
        if (application == null)
        {
            return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Application not found", new() { "No application found with this ID" });
        }

        if (application.ApplicantId == null)
        {
            return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Applicant not found", new() { "This application has no associated applicant" });
        }

        if (request.UserRole == "applicant" && request.UserId.HasValue)
        {
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId.Value);
            if (applicant == null || applicant.ApplicantId != application.ApplicantId)
            {
                return BaseResponse<ApplicationWithDocumentsDto>.FailureResponse("Forbidden", new() { "You are not authorized to view this application." });
            }
        }

        var documents = await _unitOfWork.Documents.GetByApplicantIdAsync(application.ApplicantId.Value);

        var dto = _mapper.Map<ApplicationWithDocumentsDto>(application);
        dto.Documents = _mapper.Map<List<DocumentDto>>(documents);

        return BaseResponse<ApplicationWithDocumentsDto>.SuccessResponse(dto, "Application and documents retrieved successfully");
    }
}
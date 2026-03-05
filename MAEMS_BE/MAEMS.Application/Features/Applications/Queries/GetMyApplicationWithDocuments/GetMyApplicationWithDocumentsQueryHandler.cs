using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplicationWithDocuments;

public class GetMyApplicationWithDocumentsQueryHandler : IRequestHandler<GetMyApplicationWithDocumentsQuery, BaseResponse<List<ApplicationWithDocumentsDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicationWithDocumentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<ApplicationWithDocumentsDto>>> Handle(GetMyApplicationWithDocumentsQuery request, CancellationToken cancellationToken)
    {
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<List<ApplicationWithDocumentsDto>>.FailureResponse("Applicant not found", new() { "No applicant profile found for this user" });
        }

        var applications = await _unitOfWork.Applications.GetAllByApplicantIdAsync(applicant.ApplicantId);
        var resultList = new List<ApplicationWithDocumentsDto>();

        foreach (var application in applications)
        {
            var documents = await _unitOfWork.Documents.GetByApplicationIdAsync(application.ApplicationId);
            var dto = _mapper.Map<ApplicationWithDocumentsDto>(application);
            dto.Documents = _mapper.Map<List<DocumentDto>>(documents);
            resultList.Add(dto);
        }

        return BaseResponse<List<ApplicationWithDocumentsDto>>.SuccessResponse(resultList, "Applications and documents retrieved successfully");
    }
}
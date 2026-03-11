using AutoMapper;
using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetApplicantDocuments;

public class GetApplicantDocumentsQueryHandler : IRequestHandler<GetApplicantDocumentsQuery, BaseResponse<List<DocumentDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetApplicantDocumentsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<DocumentDto>>> Handle(GetApplicantDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await _unitOfWork.Documents.GetByApplicantIdAsync(request.ApplicantId);
        var dtos = _mapper.Map<List<DocumentDto>>(documents);
        return BaseResponse<List<DocumentDto>>.SuccessResponse(dtos, "Documents retrieved successfully");
    }
}
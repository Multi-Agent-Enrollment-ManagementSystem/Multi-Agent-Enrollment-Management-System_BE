using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Documents.Commands.DeleteDocument;

public sealed class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDocumentCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var doc = await _unitOfWork.Documents.GetByIdAsync(request.DocumentId);
        if (doc == null)
        {
            return BaseResponse<bool>.FailureResponse(
                "Document not found",
                new List<string> { $"Document with ID {request.DocumentId} does not exist" });
        }

        // Only author (owner applicant) can delete
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<bool>.FailureResponse(
                "Applicant profile not found",
                new List<string> { "Please create applicant profile first" });
        }

        if (doc.ApplicantId != applicant.ApplicantId)
        {
            return BaseResponse<bool>.FailureResponse(
                "Forbidden",
                new List<string> { "You are not allowed to delete this document" });
        }

        // Set AgentLog.DocumentId = null for logs referencing this document
        // Use transaction to keep consistency
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var logsReferencingDoc = await _unitOfWork.AgentLogs
                .FindAsync(l => l.DocumentId == request.DocumentId);

            foreach (var log in logsReferencingDoc)
            {
                log.DocumentId = null;
                await _unitOfWork.AgentLogs.UpdateAsync(log);
            }

            await _unitOfWork.Documents.DeleteAsync(doc);


            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return BaseResponse<bool>.SuccessResponse(true, "Document deleted successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return BaseResponse<bool>.FailureResponse(
                "Error deleting document",
                new List<string> { ex.Message });
        }
    }
}

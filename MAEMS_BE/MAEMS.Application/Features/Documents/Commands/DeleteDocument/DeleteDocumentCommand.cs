using MediatR;
using MAEMS.Domain.Common;

namespace MAEMS.Application.Features.Documents.Commands.DeleteDocument;

public sealed record DeleteDocumentCommand(int DocumentId, int UserId) : IRequest<BaseResponse<bool>>;

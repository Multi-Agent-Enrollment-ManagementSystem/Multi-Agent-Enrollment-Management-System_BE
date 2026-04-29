using AutoMapper;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Articles.Commands.PatchArticle;

public class PatchArticleCommandHandler : IRequestHandler<PatchArticleCommand, BaseResponse<ArticleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public PatchArticleCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ArticleDto>> Handle(PatchArticleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _unitOfWork.Articles.GetByIdAsync(request.ArticleId);
            if (article == null)
            {
                return BaseResponse<ArticleDto>.FailureResponse(
                    $"Article with ID {request.ArticleId} not found",
                    new List<string> { "Article not found" });
            }

            var changed = false;

            if (request.Title != null)
            {
                var title = request.Title.Trim();
                if (string.IsNullOrWhiteSpace(title))
                    return BaseResponse<ArticleDto>.FailureResponse("Title cannot be empty", new List<string> { "Title cannot be empty" });

                article.Title = title;
                changed = true;
            }

            if (request.Content != null)
            {
                var content = request.Content;
                if (string.IsNullOrWhiteSpace(content))
                    return BaseResponse<ArticleDto>.FailureResponse("Content cannot be empty", new List<string> { "Content cannot be empty" });

                article.Content = content;
                changed = true;
            }

            if (request.Thumbnail != null)
            {
                article.Thumbnail = request.Thumbnail;
                changed = true;
            }

            if (request.Status != null)
            {
                var status = request.Status.Trim();
                if (string.IsNullOrWhiteSpace(status))
                    return BaseResponse<ArticleDto>.FailureResponse("Status cannot be empty", new List<string> { "Status cannot be empty" });

                article.Status = status;
                changed = true;
            }

            if (request.IsRegisterable != null)
            {
                article.IsRegisterable = request.IsRegisterable;
                changed = true;
            }

            if (!changed)
            {
                var dtoNoChange = _mapper.Map<ArticleDto>(article);
                return BaseResponse<ArticleDto>.SuccessResponse(dtoNoChange, "No changes applied");
            }

            article.UpdatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            await _unitOfWork.Articles.UpdateAsync(article);
            await _unitOfWork.SaveChangesAsync();

            var (updated, authorname) = await _unitOfWork.Articles.GetArticleWithAuthornameByIdAsync(article.ArticleId, cancellationToken);
            if (updated == null)
                return BaseResponse<ArticleDto>.FailureResponse("Article not found", new List<string> { "Article not found" });

            var dto = _mapper.Map<ArticleDto>(updated);
            dto.Authorname = authorname;

            return BaseResponse<ArticleDto>.SuccessResponse(dto, "Article updated successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ArticleDto>.FailureResponse("Error updating article", new List<string> { ex.Message });
        }
    }
}

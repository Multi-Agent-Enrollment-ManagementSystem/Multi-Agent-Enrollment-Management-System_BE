using AutoMapper;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Articles.Commands.CreateArticle;

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, BaseResponse<ArticleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateArticleCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ArticleDto>> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BaseResponse<ArticleDto>.FailureResponse("Title is required", new List<string> { "Title is required" });

        if (string.IsNullOrWhiteSpace(request.Content))
            return BaseResponse<ArticleDto>.FailureResponse("Content is required", new List<string> { "Content is required" });

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        var article = new Article
        {
            Title = request.Title.Trim(),
            Content = request.Content,
            Thumbnail = request.Thumbnail,
            AuthorId = request.AuthorId,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.Articles.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<ArticleDto>(article);
        return BaseResponse<ArticleDto>.SuccessResponse(dto, "Article created successfully");
    }
}

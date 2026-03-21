using AutoMapper;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Entities;

namespace MAEMS.Application.Mappings;

public class ArticleProfile : Profile
{
    public ArticleProfile()
    {
        CreateMap<Article, ArticleDto>()
            .ForMember(d => d.Authorname, opt => opt.Ignore());

        CreateMap<Article, ArticleBasicDto>();
    }
}

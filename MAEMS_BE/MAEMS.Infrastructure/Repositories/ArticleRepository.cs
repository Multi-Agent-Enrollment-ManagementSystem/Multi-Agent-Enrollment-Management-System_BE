using System.Linq.Expressions;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using DomainArticle = MAEMS.Domain.Entities.Article;
using InfraArticle = MAEMS.Infrastructure.Models.Article;

namespace MAEMS.Infrastructure.Repositories;

public class ArticleRepository : BaseRepository, IArticleRepository
{
    public ArticleRepository(postgresContext context) : base(context) { }

    public async Task<DomainArticle?> GetByIdAsync(int id)
    {
        var infra = await _context.Articles.FindAsync(id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainArticle>> GetAllAsync()
    {
        var infra = await _context.Articles.AsNoTracking().ToListAsync();
        return infra.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainArticle>> FindAsync(Expression<Func<DomainArticle, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainArticle> AddAsync(DomainArticle entity)
    {
        var infra = new InfraArticle
        {
            Title = entity.Title,
            Content = entity.Content,
            Thumbnail = entity.Thumbnail,
            AuthorId = entity.AuthorId,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

        await _context.Articles.AddAsync(infra);
        entity.ArticleId = infra.ArticleId;
        return entity;
    }

    public async Task UpdateAsync(DomainArticle entity)
    {
        var infra = await _context.Articles.FindAsync(entity.ArticleId);
        if (infra == null) return;

        infra.Title = entity.Title;
        infra.Content = entity.Content;
        infra.Thumbnail = entity.Thumbnail;
        infra.AuthorId = entity.AuthorId;
        infra.Status = entity.Status;
        infra.UpdatedAt = entity.UpdatedAt;

        _context.Articles.Update(infra);
    }

    public async Task DeleteAsync(DomainArticle entity)
    {
        var infra = await _context.Articles.FindAsync(entity.ArticleId);
        if (infra != null)
        {
            _context.Articles.Remove(infra);
        }
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainArticle, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    public async Task<(IReadOnlyList<DomainArticle> Items, int TotalCount)> GetPublishedArticlesBasicPagedAsync(
        string? searchTitle,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Articles
            .AsNoTracking()
            .Where(a => a.Status != null && a.Status.ToLower() == "publish");

        if (!string.IsNullOrWhiteSpace(searchTitle))
        {
            query = query.Where(a => EF.Functions.ILike(a.Title!, $"%{searchTitle.Trim()}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "updatedat" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "articleid" => sortDesc ? query.OrderByDescending(x => x.ArticleId) : query.OrderBy(x => x.ArticleId),
            "title" => sortDesc ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "updatedat" or _ => sortDesc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.ArticleId)
                : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.ArticleId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Map only basic fields; domain entity supports more but this is still light.
        var domainItems = items.Select(MapToDomain).ToList();
        return (domainItems, totalCount);
    }

    public async Task<(IReadOnlyList<DomainArticle> Items, int TotalCount)> GetArticlesBasicPagedAsync(
        string? searchTitle,
        string? status,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Articles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTitle))
        {
            query = query.Where(a => EF.Functions.ILike(a.Title!, $"%{searchTitle.Trim()}%"));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var st = status.Trim();
            query = query.Where(a => a.Status != null && a.Status.ToLower() == st.ToLower());
        }

        var totalCount = await query.CountAsync(cancellationToken);

        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "updatedat" : sortBy.Trim();
        query = sortBy.ToLowerInvariant() switch
        {
            "articleid" => sortDesc ? query.OrderByDescending(x => x.ArticleId) : query.OrderBy(x => x.ArticleId),
            "title" => sortDesc ? query.OrderByDescending(x => x.Title) : query.OrderBy(x => x.Title),
            "status" => sortDesc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "updatedat" or _ => sortDesc
                ? query.OrderByDescending(x => x.UpdatedAt).ThenByDescending(x => x.ArticleId)
                : query.OrderBy(x => x.UpdatedAt).ThenBy(x => x.ArticleId)
        };

        var skip = (pageNumber - 1) * pageSize;
        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items.Select(MapToDomain).ToList(), totalCount);
    }

    public async Task<(DomainArticle? Article, string? Authorname)> GetArticleWithAuthornameByIdAsync(
        int articleId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.Articles
            .AsNoTracking()
            .Where(a => a.ArticleId == articleId)
            .Select(a => new
            {
                Article = a,
                Authorname = a.Author != null ? a.Author.Username : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return (null, null);

        return (MapToDomain(result.Article), result.Authorname);
    }

    private static DomainArticle MapToDomain(InfraArticle infra)
    {
        return new DomainArticle
        {
            ArticleId = infra.ArticleId,
            Title = infra.Title,
            Content = infra.Content,
            Thumbnail = infra.Thumbnail,
            AuthorId = infra.AuthorId,
            Status = infra.Status,
            CreatedAt = infra.CreatedAt,
            UpdatedAt = infra.UpdatedAt
        };
    }
}

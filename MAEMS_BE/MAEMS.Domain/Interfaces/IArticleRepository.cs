using System.Linq.Expressions;
using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(int id);
    Task<IEnumerable<Article>> GetAllAsync();
    Task<IEnumerable<Article>> FindAsync(Expression<Func<Article, bool>> predicate);
    Task<Article> AddAsync(Article entity);
    Task UpdateAsync(Article entity);
    Task DeleteAsync(Article entity);
    Task<bool> ExistsAsync(Expression<Func<Article, bool>> predicate);

    // SQL-level filtering/sorting/paging for published article basic listing.
    Task<(IReadOnlyList<Article> Items, int TotalCount)> GetPublishedArticlesBasicPagedAsync(
        string? searchTitle,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // SQL-level filtering/sorting/paging for admin basic listing (no forced status).
    Task<(IReadOnlyList<Article> Items, int TotalCount)> GetArticlesBasicPagedAsync(
        string? searchTitle,
        string? status,
        string? sortBy,
        bool sortDesc,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // SQL-level load article by id including author (username) for DTO.
    Task<(Article? Article, string? Authorname)> GetArticleWithAuthornameByIdAsync(
        int articleId,
        CancellationToken cancellationToken = default);
}

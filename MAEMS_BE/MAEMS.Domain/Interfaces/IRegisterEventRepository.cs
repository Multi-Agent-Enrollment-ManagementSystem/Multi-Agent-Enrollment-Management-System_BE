using MAEMS.Domain.Entities;
using System.Linq.Expressions;

namespace MAEMS.Domain.Interfaces;

public interface IRegisterEventRepository
{
    Task<RegisterEvent?> GetByIdAsync(int id);
    Task<IEnumerable<RegisterEvent>> GetAllAsync();
    Task<IEnumerable<RegisterEvent>> GetByArticleIdAsync(int articleId);
    Task<IEnumerable<RegisterEvent>> FindAsync(Expression<Func<RegisterEvent, bool>> predicate);
    Task<RegisterEvent> AddAsync(RegisterEvent entity);
    Task UpdateAsync(RegisterEvent entity);
    Task DeleteAsync(RegisterEvent entity);
    Task<bool> ExistsAsync(Expression<Func<RegisterEvent, bool>> predicate);
}
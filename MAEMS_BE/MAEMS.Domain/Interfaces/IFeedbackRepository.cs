using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IFeedbackRepository : IGenericRepository<Feedback>
{
    Task<IEnumerable<Feedback>> GetAllWithUserAsync();
    Task<(IEnumerable<Feedback> Items, int TotalCount)> GetPagedWithUserAsync(int pageNumber, int pageSize);
}
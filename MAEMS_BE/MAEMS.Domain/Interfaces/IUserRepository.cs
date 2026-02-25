namespace MAEMS.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<Entities.User>
{
    Task<Entities.User?> GetByUsernameAsync(string username);
    Task<Entities.User?> GetByEmailAsync(string email);
    Task<bool> IsUsernameExistsAsync(string username);
    Task<bool> IsEmailExistsAsync(string email);
}

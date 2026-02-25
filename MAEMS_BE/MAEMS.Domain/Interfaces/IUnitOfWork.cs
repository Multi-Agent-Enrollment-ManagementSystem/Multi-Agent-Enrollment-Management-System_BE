namespace MAEMS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRoleRepository Roles { get; }
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

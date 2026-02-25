using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace MAEMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly postgresContext _context;
    private IDbContextTransaction? _transaction;
    private IRoleRepository? _roleRepository;
    private IUserRepository? _userRepository;
    private IMajorRepository? _majorRepository;
    private IProgramRepository? _programRepository;

    public UnitOfWork(postgresContext context)
    {
        _context = context;
    }

    public IRoleRepository Roles
    {
        get
        {
            return _roleRepository ??= new RoleRepository(_context);
        }
    }

    public IUserRepository Users
    {
        get
        {
            return _userRepository ??= new UserRepository(_context);
        }
    }

    public IMajorRepository Majors
    {
        get
        {
            return _majorRepository ??= new MajorRepository(_context);
        }
    }

    public IProgramRepository Programs
    {
        get
        {
            return _programRepository ??= new ProgramRepository(_context);
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

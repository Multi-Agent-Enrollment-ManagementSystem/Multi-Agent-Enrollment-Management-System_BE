using MAEMS.Domain.Interfaces;
using MAEMS.Infrastructure.Models;
using MAEMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace MAEMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly postgresContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(postgresContext context)
    {
        _context = context;
    }

    public IRoleRepository Roles => new RoleRepository(_context);
    public IUserRepository Users => new UserRepository(_context);
    public IMajorRepository Majors => new MajorRepository(_context);
    public IProgramRepository Programs => new ProgramRepository(_context);
    public ICampusRepository Campuses => new CampusRepository(_context);
    public IApplicantRepository Applicants => new ApplicantRepository(_context);
    public IAdmissionTypeRepository AdmissionTypes => new AdmissionTypeRepository(_context);
    public IApplicationRepository Applications => new ApplicationRepository(_context);
    public IDocumentRepository Documents => new DocumentRepository(_context); // Added

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

namespace MAEMS.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRoleRepository Roles { get; }
    IUserRepository Users { get; }
    IMajorRepository Majors { get; }
    IProgramRepository Programs { get; }
    ICampusRepository Campuses { get; }
    IApplicantRepository Applicants { get; }
    IAdmissionTypeRepository AdmissionTypes { get; }
    IApplicationRepository Applications { get; } // Added
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

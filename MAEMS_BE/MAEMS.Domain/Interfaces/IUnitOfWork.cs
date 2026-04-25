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
    IApplicationRepository Applications { get; }
    IDocumentRepository Documents { get; }
    IProgramAdmissionConfigRepository ProgramAdmissionConfigs { get; }
    IEnrollmentYearRepository EnrollmentYears { get; }
    IFeedbackRepository Feedbacks { get; }

    IAgentLogRepository AgentLogs { get; }
    ILlmChatLogRepository LlmChatLogs { get; }

    IArticleRepository Articles { get; }

    INotificationRepository Notifications { get; }

    IPaymentRepository Payments { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

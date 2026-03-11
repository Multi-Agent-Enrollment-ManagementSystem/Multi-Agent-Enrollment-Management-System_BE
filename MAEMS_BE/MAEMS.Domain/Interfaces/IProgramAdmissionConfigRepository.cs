using MAEMS.Domain.Entities;
using System.Linq.Expressions;

namespace MAEMS.Domain.Interfaces;

public interface IProgramAdmissionConfigRepository : IGenericRepository<ProgramAdmissionConfig>
{
    Task<IEnumerable<ProgramAdmissionConfig>> GetActiveConfigsAsync();
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByProgramIdAsync(int programId);
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByCampusIdAsync(int campusId);
    Task<IEnumerable<ProgramAdmissionConfig>> GetConfigsByAdmissionTypeIdAsync(int admissionTypeId);
}

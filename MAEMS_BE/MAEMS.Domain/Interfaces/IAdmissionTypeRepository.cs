using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IAdmissionTypeRepository : IGenericRepository<AdmissionType>
{
    Task<IEnumerable<AdmissionType>> GetActiveAdmissionTypesAsync();
    Task<IEnumerable<AdmissionType>> GetAdmissionTypesByEnrollmentYearIdAsync(int enrollmentYearId);
}

using System.Threading.Tasks;
using MAEMS.Domain.Entities;

namespace MAEMS.Domain.Interfaces;

public interface IScoreRepository
{
    Task<Score?> GetByApplicantIdAsync(int applicantId);
    Task<Score> AddAsync(Score score);
    Task UpdateAsync(Score score);
}
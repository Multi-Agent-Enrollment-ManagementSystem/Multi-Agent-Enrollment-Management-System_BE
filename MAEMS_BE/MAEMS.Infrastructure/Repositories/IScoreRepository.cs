using MAEMS.Infrastructure.Models;
using System.Threading.Tasks;

namespace MAEMS.Infrastructure.Repositories;

public interface IScoreRepository
{
    Task<Score> GetByApplicantIdAsync(int applicantId);
    Task<Score> AddAsync(Score score);
    Task UpdateAsync(Score score);
}
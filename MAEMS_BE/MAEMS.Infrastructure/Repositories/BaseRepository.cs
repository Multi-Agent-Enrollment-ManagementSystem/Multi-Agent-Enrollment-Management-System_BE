using MAEMS.Infrastructure.Models;

namespace MAEMS.Infrastructure.Repositories;

public abstract class BaseRepository
{
    protected readonly postgresContext _context;

    protected BaseRepository(postgresContext context)
    {
        _context = context;
    }
}
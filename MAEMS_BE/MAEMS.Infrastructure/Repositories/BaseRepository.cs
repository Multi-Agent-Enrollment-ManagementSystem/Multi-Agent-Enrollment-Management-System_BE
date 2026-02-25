using MAEMS.Infrastructure.Models;

namespace MAEMS.Infrastructure.Repositories;

/// <summary>
/// Base repository that provides access to the database context.
/// All specific repositories should inherit from this class.
/// </summary>
public abstract class BaseRepository
{
    protected readonly postgresContext _context;

    protected BaseRepository(postgresContext context)
    {
        _context = context;
    }
}

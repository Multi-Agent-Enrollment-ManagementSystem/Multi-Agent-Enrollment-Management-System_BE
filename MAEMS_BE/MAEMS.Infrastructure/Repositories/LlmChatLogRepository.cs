using MAEMS.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace MAEMS.Infrastructure.Repositories;

public interface ILlmChatLogRepository
{
    Task<LlmChatLog> AddAsync(LlmChatLog chatLog, CancellationToken cancellationToken = default);
    Task<List<LlmChatLog>> GetByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<LlmChatLog?> GetByIdAsync(int chatId, CancellationToken cancellationToken = default);
}

public class LlmChatLogRepository : ILlmChatLogRepository
{
    private readonly postgresContext _context;

    public LlmChatLogRepository(postgresContext context)
    {
        _context = context;
    }

    public async Task<LlmChatLog> AddAsync(LlmChatLog chatLog, CancellationToken cancellationToken = default)
    {
        _context.LlmChatLogs.Add(chatLog);
        await _context.SaveChangesAsync(cancellationToken);
        return chatLog;
    }

    public async Task<List<LlmChatLog>> GetByUserIdAsync(int userId, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return await _context.LlmChatLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.LlmChatLogs
            .Where(x => x.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<LlmChatLog?> GetByIdAsync(int chatId, CancellationToken cancellationToken = default)
    {
        return await _context.LlmChatLogs
            .FirstOrDefaultAsync(x => x.ChatId == chatId, cancellationToken);
    }
}

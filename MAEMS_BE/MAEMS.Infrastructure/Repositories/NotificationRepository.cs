using System.Linq.Expressions;
using DomainNotification = MAEMS.Domain.Entities.Notification;
using InfraNotification = MAEMS.Infrastructure.Models.Notification;
using MAEMS.Domain.Interfaces;

namespace MAEMS.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly Models.postgresContext _context;

    public NotificationRepository(Models.postgresContext context)
    {
        _context = context;
    }

    public async Task<DomainNotification?> GetByIdAsync(int id)
    {
        var infra = await _context.Notifications.FindAsync(id);
        return infra == null ? null : MapToDomain(infra);
    }

    public async Task<IEnumerable<DomainNotification>> GetAllAsync()
    {
        var infraList = await Task.FromResult(_context.Notifications.ToList());
        return infraList.Select(MapToDomain);
    }

    public async Task<IEnumerable<DomainNotification>> FindAsync(Expression<Func<DomainNotification, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate.Compile());
    }

    public async Task<DomainNotification> AddAsync(DomainNotification entity)
    {
        var infra = MapToInfra(entity);
        _context.Notifications.Add(infra);
        await _context.SaveChangesAsync();
        entity.NotificationId = infra.NotificationId;
        return entity;
    }

    public async Task UpdateAsync(DomainNotification entity)
    {
        var infra = await _context.Notifications.FindAsync(entity.NotificationId);
        if (infra != null)
        {
            infra.RecipientUserId = entity.RecipientUserId;
            infra.NotificationType = entity.NotificationType;
            infra.Message = entity.Message;
            infra.IsRead = entity.IsRead;
            infra.SentAt = entity.SentAt;
            _context.Notifications.Update(infra);
        }

        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DomainNotification entity)
    {
        var infra = await _context.Notifications.FindAsync(entity.NotificationId);
        if (infra != null)
        {
            _context.Notifications.Remove(infra);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(Expression<Func<DomainNotification, bool>> predicate)
    {
        var all = await GetAllAsync();
        return all.Any(predicate.Compile());
    }

    private static DomainNotification MapToDomain(InfraNotification infra)
    {
        return new DomainNotification
        {
            NotificationId = infra.NotificationId,
            RecipientUserId = infra.RecipientUserId,
            NotificationType = infra.NotificationType ?? string.Empty,
            Message = infra.Message ?? string.Empty,
            IsRead = infra.IsRead,
            SentAt = infra.SentAt
        };
    }

    private static InfraNotification MapToInfra(DomainNotification domain)
    {
        return new InfraNotification
        {
            NotificationId = domain.NotificationId,
            RecipientUserId = domain.RecipientUserId,
            NotificationType = domain.NotificationType,
            Message = domain.Message,
            IsRead = domain.IsRead,
            SentAt = domain.SentAt,
            RecipientUser = null!
        };
    }
}

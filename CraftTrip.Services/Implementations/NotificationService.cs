using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly CraftTripDbContext _context;

    public NotificationService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task SendNotificationAsync(Guid userId, string title, string message, string? actionUrl = null, string type = "General")
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message.Trim(),
            ActionUrl = actionUrl,
            Type = type,
            IsRead = false
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 10)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
    {
        var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notif == null) return false;

        notif.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }
}

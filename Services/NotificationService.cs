using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Services;

public interface INotificationService
{
    Task CreateAsync(string userId, string title, string message, NotificationType type, string? actionUrl = null);
    Task NotifyRoleAsync(string roleName, string title, string message, NotificationType type, string? actionUrl = null);
    Task<int> GetUnreadCountAsync(string userId);
    Task<List<Notification>> GetRecentAsync(string userId, int count = 5);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task CreateAsync(string userId, string title, string message,
        NotificationType type, string? actionUrl = null)
    {
        _context.Notifications.Add(Build(userId, title, message, type, actionUrl));
        await _context.SaveChangesAsync();
    }

    /// <summary>Sends the same notification to every user in a role (e.g. all Admins).</summary>
    public async Task NotifyRoleAsync(string roleName, string title, string message,
        NotificationType type, string? actionUrl = null)
    {
        var users = await _userManager.GetUsersInRoleAsync(roleName);
        if (users.Count == 0) return;

        foreach (var user in users)
        {
            _context.Notifications.Add(Build(user.Id, title, message, type, actionUrl));
        }

        await _context.SaveChangesAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public Task<List<Notification>> GetRecentAsync(string userId, int count = 5) =>
        _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        // The UserId check stops one user from touching another user's notifications
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null || notification.IsRead) return;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    private static Notification Build(string userId, string title, string message,
        NotificationType type, string? actionUrl) => new()
        {
            UserId = userId,
            Title = title.Length <= 100 ? title : title[..100],
            Message = message.Length <= 500 ? message : message[..500],
            Type = type,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
}
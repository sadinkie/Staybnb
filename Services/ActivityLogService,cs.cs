using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Services;

public interface IActivityLogService
{
    Task LogAsync(string? userId, string action, ActivityType type, string? details = null);
}

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _context;

    public ActivityLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string action, ActivityType type, string? details = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = Truncate(action, 100),
            ActivityType = type,
            Details = details is null ? null : Truncate(details, 500),
            CreatedAt = DateTime.UtcNow
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
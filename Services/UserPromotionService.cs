using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Services;

public interface IUserPromotionService
{
    Task<OperationResult> PromoteGuestToAdminAsync(string targetUserId, ApplicationUser actor);
}

public class UserPromotionService : IUserPromotionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;
    private readonly IActivityLogService _activityLog;

    public UserPromotionService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications,
        IActivityLogService activityLog)
    {
        _context = context;
        _userManager = userManager;
        _notifications = notifications;
        _activityLog = activityLog;
    }

    public async Task<OperationResult> PromoteGuestToAdminAsync(string targetUserId, ApplicationUser actor)
    {
        // Defence in depth: the controller already requires SuperAdmin, and the service checks again
        if (!await _userManager.IsInRoleAsync(actor, RoleNames.SuperAdmin))
            return OperationResult.Fail("Only the SuperAdmin can assign the Admin role.");

        var target = await _userManager.FindByIdAsync(targetUserId);
        if (target is null)
            return OperationResult.Fail("That user could not be found.");

        if (!await _userManager.IsInRoleAsync(target, RoleNames.Guest))
            return OperationResult.Fail($"{target.FullName} is not a Guest, so they cannot be promoted. Only Guests can become Admins.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var add = await _userManager.AddToRoleAsync(target, RoleNames.Admin);
            if (!add.Succeeded)
                throw new InvalidOperationException(string.Join("; ", add.Errors.Select(e => e.Description)));

            var remove = await _userManager.RemoveFromRoleAsync(target, RoleNames.Guest);
            if (!remove.Succeeded)
                throw new InvalidOperationException(string.Join("; ", remove.Errors.Select(e => e.Description)));

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail($"The promotion failed: {ex.Message}");
        }

        await _notifications.CreateAsync(
            target.Id,
            "You are now an Admin",
            "The SuperAdmin promoted your account to Admin. You can now review host applications and the activity log.",
            NotificationType.System,
            "/Admin/Dashboard");

        await _activityLog.LogAsync(target.Id, "PromotedToAdmin", ActivityType.RoleChange,
            $"{target.Email}: Guest -> Admin (promoted by {actor.Email}).");

        return OperationResult.Success($"{target.FullName} is now an Admin.");
    }
}
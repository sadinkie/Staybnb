using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.Services;

public interface IHostApplicationReviewService
{
    Task<OperationResult> ApproveAsync(int applicationId, ApplicationUser admin);
    Task<OperationResult> RejectAsync(int applicationId, ApplicationUser admin, string? notes);
}

public class HostApplicationReviewService : IHostApplicationReviewService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;
    private readonly IActivityLogService _activityLog;

    public HostApplicationReviewService(
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

    // ------------------------------------------------------------------
    // APPROVE: approve the application and property, then swap Guest -> Host
    // ------------------------------------------------------------------
    public async Task<OperationResult> ApproveAsync(int applicationId, ApplicationUser admin)
    {
        var application = await _context.HostApplications
            .Include(a => a.Property)
            .Include(a => a.ApplicationUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return OperationResult.Fail("That application could not be found.");

        if (application.Status != ApplicationStatus.Pending)
            return OperationResult.Fail("This application has already been reviewed.");

        var applicant = application.ApplicationUser;
        var title = application.Property.Title;
        var roleChanged = false;

        // One transaction: the application update AND the role swap succeed together, or not at all
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            application.Status = ApplicationStatus.Approved;
            application.ReviewedAt = DateTime.UtcNow;
            application.ReviewedById = admin.Id;
            application.Property.IsApproved = true;
            application.Property.IsActive = true;
            await _context.SaveChangesAsync();

            // THE ROLE SWAP: 1. add Host
            if (!await _userManager.IsInRoleAsync(applicant, RoleNames.Host))
            {
                EnsureSucceeded(await _userManager.AddToRoleAsync(applicant, RoleNames.Host));
                roleChanged = true;
            }

            // THE ROLE SWAP: 2. remove Guest (a user is a Guest or a Host, never both)
            if (await _userManager.IsInRoleAsync(applicant, RoleNames.Guest))
            {
                EnsureSucceeded(await _userManager.RemoveFromRoleAsync(applicant, RoleNames.Guest));
                roleChanged = true;
            }

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return OperationResult.Fail($"The application could not be approved: {ex.Message}");
        }

        // Notifications and logging happen after the commit
        await _notifications.CreateAsync(
            applicant.Id,
            "Host Application Approved",
            roleChanged
                ? $"Congratulations! Your application for '{title}' was approved and you are now a Host."
                : $"Your additional property '{title}' was approved and is now live.",
            NotificationType.HostApplication,
            roleChanged ? "/Host/Dashboard" : "/Host/Properties");

        if (roleChanged)
        {
            await _activityLog.LogAsync(applicant.Id, "RoleChanged", ActivityType.RoleChange,
                $"{applicant.Email}: Guest -> Host (application #{application.Id} approved by {admin.Email}).");
        }

        await _activityLog.LogAsync(admin.Id, "HostApplicationApproved", ActivityType.HostApplication,
            $"Approved '{title}' by {applicant.Email} (application #{application.Id}).");

        return OperationResult.Success(roleChanged
            ? $"{applicant.FullName} is now a Host and '{title}' is live."
            : $"'{title}' was approved and is now live.");
    }

    // ------------------------------------------------------------------
    // REJECT: a reason is mandatory; roles are not touched
    // ------------------------------------------------------------------
    public async Task<OperationResult> RejectAsync(int applicationId, ApplicationUser admin, string? notes)
    {
        notes = notes?.Trim();

        if (string.IsNullOrWhiteSpace(notes))
            return OperationResult.Fail("Please give the applicant a reason for rejecting their application.");

        if (notes.Length > 500)
            notes = notes[..500];

        var application = await _context.HostApplications
            .Include(a => a.Property)
            .Include(a => a.ApplicationUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
            return OperationResult.Fail("That application could not be found.");

        if (application.Status != ApplicationStatus.Pending)
            return OperationResult.Fail("This application has already been reviewed.");

        application.Status = ApplicationStatus.Rejected;
        application.ReviewedAt = DateTime.UtcNow;
        application.ReviewedById = admin.Id;
        application.AdminNotes = notes;
        await _context.SaveChangesAsync();

        var applicant = application.ApplicationUser;
        var title = application.Property.Title;

        await _notifications.CreateAsync(
            applicant.Id,
            "Host Application Rejected",
            $"Your application for '{title}' was not approved. Reason: {notes}",
            NotificationType.HostApplication,
            "/BecomeHost/Status");

        await _activityLog.LogAsync(admin.Id, "HostApplicationRejected", ActivityType.HostApplication,
            $"Rejected '{title}' by {applicant.Email} (application #{application.Id}).");

        return OperationResult.Success($"The application for '{title}' was rejected and {applicant.FirstName} has been notified.");
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
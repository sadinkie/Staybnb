
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;

namespace Staybnb.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = RoleNames.Host)]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notifications;

    public BookingsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLog,
        INotificationService notifications)
    {
        _context = context;
        _userManager = userManager;
        _activityLog = activityLog;
        _notifications = notifications;
    }

    // GET: /Host/Bookings
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .Where(b => b.Property.HostId == hostId)
            .OrderBy(b => b.Status == BookingStatus.Pending ? 0 : 1)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }

    // POST: /Host/Bookings/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        return await DecideAsync(id, BookingStatus.Approved);
    }

    // POST: /Host/Bookings/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        return await DecideAsync(id, BookingStatus.Rejected);
    }

    private async Task<IActionResult> DecideAsync(
        int id, BookingStatus decision)
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        // Use a transaction to keep the decision, audit log,
        // and notification together.
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

        var booking = await _context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b =>
                b.Id == id && b.Property.HostId == hostId);

        if (booking is null)
        {
            await transaction.RollbackAsync();
            return NotFound();
        }

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["Error"] =
                "Only pending bookings can be approved or rejected.";
            return RedirectToAction(nameof(Index));
        }

        if (decision == BookingStatus.Approved)
        {
            // Prevent overlapping approved stays for this property.
            var conflict = await _context.Bookings.AnyAsync(b =>
                b.PropertyId == booking.PropertyId &&
                b.Id != booking.Id &&
                (b.Status == BookingStatus.Approved ||
                 b.Status == BookingStatus.CheckedIn ||
                 b.Status == BookingStatus.Completed) &&
                b.CheckInDate < booking.CheckOutDate &&
                b.CheckOutDate > booking.CheckInDate);

            if (conflict)
            {
                TempData["Error"] =
                    "This property already has a confirmed booking " +
                    "that overlaps the requested dates.";

                return RedirectToAction(nameof(Index));
            }
        }

        booking.Status = decision;
        await _context.SaveChangesAsync();

        var approved = decision == BookingStatus.Approved;

        await _activityLog.LogAsync(
            hostId,
            approved ? "BookingApproved" : "BookingRejected",
            ActivityType.BookingStatusChange,
            $"Host {(approved ? "approved" : "rejected")} " +
            $"booking #{booking.Id} for property #{booking.PropertyId}.");

        await _notifications.CreateAsync(
            booking.GuestId,
            approved ? "Booking approved" : "Booking rejected",
            approved
                ? $"Your booking for {booking.Property.Title} has been approved."
                : $"Your booking for {booking.Property.Title} has been rejected.",
            NotificationType.Booking);

        await transaction.CommitAsync();

        TempData["Success"] = approved
            ? "Booking approved successfully."
            : "Booking rejected successfully.";

        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.AdminOrSuperAdmin)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            TotalUsers = await _userManager.Users.CountAsync(),

            PendingHostApplications = await _context.HostApplications
                .CountAsync(a => a.Status == ApplicationStatus.Pending),

            ActiveProperties = await _context.HostProperties
                .CountAsync(p => p.IsApproved && p.IsActive),

            // Platform revenue = service fees earned on confirmed bookings
            PlatformRevenue = await _context.Bookings
                .Where(b => b.Status == BookingStatus.Approved
                         || b.Status == BookingStatus.CheckedIn
                         || b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.ServiceFee) ?? 0,

            TotalBookings = await _context.Bookings.CountAsync(),
            PendingBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending),
            ApprovedHosts = (await _userManager.GetUsersInRoleAsync(RoleNames.Host)).Count,

            RecentActivity = await _context.ActivityLogs
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
                .Take(6)
                .Select(l => new ActivityLogItem
                {
                    Id = l.Id,
                    UserName = l.User != null ? l.User.FirstName + " " + l.User.LastName : "System",
                    UserEmail = l.User != null ? l.User.Email : null,
                    Action = l.Action,
                    ActivityType = l.ActivityType,
                    Details = l.Details,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync(),

            RecentApplications = await _context.HostApplications
                .AsNoTracking()
                .OrderByDescending(a => a.AppliedAt)
                .Take(4)
                .ToListItems()
                .ToListAsync(),

            RecentBookings = await _context.Bookings
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedAt)
                .Take(4)
                .Select(b => new RecentBookingItem
                {
                    GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                    GuestEmail = b.Guest.Email,
                    CheckIn = b.CheckInDate,
                    CheckOut = b.CheckOutDate,
                    Status = b.Status
                })
                .ToListAsync()
        };

        return View(model);
    }
}
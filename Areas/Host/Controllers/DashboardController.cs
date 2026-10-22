using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = RoleNames.Host)]
public class DashboardController : Controller
{
    private const int OccupancyWindowDays = 30;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        var hostId = host.Id;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var properties = _context.HostProperties.AsNoTracking().Where(p => p.HostId == hostId);
        var bookings = _context.Bookings.AsNoTracking().Where(b => b.Property.HostId == hostId);

        // Bookings that actually earn money
        var confirmed = bookings.Where(b => b.Status == BookingStatus.Approved
                                         || b.Status == BookingStatus.CheckedIn
                                         || b.Status == BookingStatus.Completed);

        var confirmedCount = await confirmed.CountAsync();
        var rejectedCount = await bookings.CountAsync(b => b.Status == BookingStatus.Rejected);
        var decidedCount = confirmedCount + rejectedCount;

        // A Host earns the price and cleaning fee. The service fee belongs to Staybnb.
        var totalEarnings = await confirmed.SumAsync(b => (decimal?)(b.TotalPrice - b.ServiceFee)) ?? 0;
        var earningsThisMonth = await confirmed
            .Where(b => b.CreatedAt >= monthStart)
            .SumAsync(b => (decimal?)(b.TotalPrice - b.ServiceFee)) ?? 0;

        var reviews = _context.Reviews.AsNoTracking().Where(r => r.Property.HostId == hostId);

        var model = new HostDashboardViewModel
        {
            FirstName = host.FirstName,

            TotalProperties = await properties.CountAsync(),
            ActiveProperties = await properties.CountAsync(p => p.IsApproved && p.IsActive),
            PendingProperties = await _context.HostApplications
                .CountAsync(a => a.ApplicationUserId == hostId && a.Status == ApplicationStatus.Pending),

            TotalBookings = await bookings.CountAsync(),
            BookingsThisMonth = await bookings.CountAsync(b => b.CreatedAt >= monthStart),
            PendingBookingRequests = await bookings.CountAsync(b => b.Status == BookingStatus.Pending),

            TotalEarnings = totalEarnings,
            EarningsThisMonth = earningsThisMonth,

            ReviewCount = await reviews.CountAsync(),
            AverageRating = await reviews.AverageAsync(r => (double?)r.Rating) ?? 0,

            AcceptanceRate = decidedCount == 0 ? 0 : confirmedCount * 100.0 / decidedCount,
            AverageNightlyRate = await properties
                .Where(p => p.IsApproved)
                .AverageAsync(p => (decimal?)p.PricePerNight) ?? 0,
            AverageBookingValue = confirmedCount == 0 ? 0 : totalEarnings / confirmedCount,

            OccupancyRate = await CalculateOccupancyAsync(properties, confirmed),

            RecentBookings = await bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new HostBookingItem
                {
                    GuestName = b.Guest.FirstName + " " + b.Guest.LastName,
                    GuestEmail = b.Guest.Email,
                    PropertyTitle = b.Property.Title,
                    CheckIn = b.CheckInDate,
                    CheckOut = b.CheckOutDate,
                    Status = b.Status,
                    Earnings = b.TotalPrice - b.ServiceFee
                })
                .ToListAsync()
        };

        return View(model);
    }

    /// <summary>Booked nights in the last 30 days as a percentage of all available nights.</summary>
    private static async Task<double> CalculateOccupancyAsync(
        IQueryable<HostProperty> properties, IQueryable<Booking> confirmed)
    {
        var approvedCount = await properties.CountAsync(p => p.IsApproved);
        if (approvedCount == 0) return 0;

        var windowEnd = DateTime.UtcNow.Date;
        var windowStart = windowEnd.AddDays(-OccupancyWindowDays);

        var stays = await confirmed
            .Where(b => b.CheckOutDate > windowStart && b.CheckInDate < windowEnd)
            .Select(b => new { b.CheckInDate, b.CheckOutDate })
            .ToListAsync();

        var bookedNights = 0;
        foreach (var stay in stays)
        {
            var from = stay.CheckInDate.Date > windowStart ? stay.CheckInDate.Date : windowStart;
            var to = stay.CheckOutDate.Date < windowEnd ? stay.CheckOutDate.Date : windowEnd;
            bookedNights += Math.Max(0, (to - from).Days);
        }

        var availableNights = approvedCount * OccupancyWindowDays;
        return Math.Min(100, bookedNights * 100.0 / availableNights);
    }
}
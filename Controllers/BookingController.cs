
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

[Authorize(Roles = RoleNames.Guest)]
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

    [HttpGet]
    public async Task<IActionResult> Create(int propertyId)
    {
        var property = await _context.HostProperties
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == propertyId &&
                p.IsApproved &&
                p.IsActive);

        if (property is null) return NotFound();

        return View(new CreateBookingViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = 0,
            MaxGuests = property.MaxGuests
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateBookingViewModel model)
    {
        var guestId = _userManager.GetUserId(User);
        if (guestId is null) return Challenge();

        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p =>
                p.Id == model.PropertyId &&
                p.IsApproved &&
                p.IsActive);

        if (property is null) return NotFound();

        // Always use the database prices, not posted form values.
        model.PropertyTitle = property.Title;
        model.PricePerNight = property.PricePerNight;
        model.CleaningFee = property.CleaningFee;
        model.ServiceFee = 0;
        model.MaxGuests = property.MaxGuests;

        if (model.CheckInDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(model.CheckInDate),
                "Check-in date cannot be in the past.");
        }

        if (model.CheckOutDate.Date <= model.CheckInDate.Date)
        {
            ModelState.AddModelError(
                nameof(model.CheckOutDate),
                "Check-out must be after check-in.");
        }

        if (model.NumberOfGuests > property.MaxGuests)
        {
            ModelState.AddModelError(
                nameof(model.NumberOfGuests),
                $"This property allows up to {property.MaxGuests} guests.");
        }

        if (!ModelState.IsValid)
            return View(model);

        // Prevent overlapping approved bookings.
        var overlap = await _context.Bookings.AnyAsync(b =>
            b.PropertyId == property.Id &&
            (b.Status == BookingStatus.Approved ||
             b.Status == BookingStatus.CheckedIn ||
             b.Status == BookingStatus.Completed) &&
            b.CheckInDate < model.CheckOutDate.Date &&
            b.CheckOutDate > model.CheckInDate.Date);

        if (overlap)
        {
            ModelState.AddModelError(
                string.Empty,
                "This property is unavailable for the selected dates.");

            return View(model);
        }

        var booking = new Booking
        {
            PropertyId = property.Id,
            GuestId = guestId,
            CheckInDate = model.CheckInDate.Date,
            CheckOutDate = model.CheckOutDate.Date,
            NumberOfGuests = model.NumberOfGuests,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = model.ServiceFee,
            TotalPrice = model.TotalPrice,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            guestId,
            "BookingCreated",
            ActivityType.BookingStatusChange,
            $"Guest created booking #{booking.Id} for property #{property.Id}.");

        await _notifications.CreateAsync(
            property.HostId,
            "New booking request",
            $"You have a new booking request for {property.Title}.",
            NotificationType.Booking,
            "/Host/Bookings");

        TempData["Success"] =
            "Your booking request has been submitted.";

        return RedirectToAction(nameof(Details),
            new { id = booking.Id });
    }
    // GET: /Bookings
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var guestId = _userManager.GetUserId(User);
        if (guestId is null)
            return Challenge();

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Where(b => b.GuestId == guestId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return View(bookings);
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var guestId = _userManager.GetUserId(User);
        if (guestId is null) return Challenge();

        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                b.GuestId == guestId);

        if (booking is null) return NotFound();

        return View(booking);
    }
}

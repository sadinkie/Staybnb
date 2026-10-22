
using System.Text.Json;
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

[Authorize]
public class GuestCheckInController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPrivateDocumentStorageService _storage;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notifications;
    private readonly ILogger<GuestCheckInController> _logger;

    public GuestCheckInController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPrivateDocumentStorageService storage,
        IActivityLogService activityLog,
        INotificationService notifications,
        ILogger<GuestCheckInController> logger)
    {
        _context = context;
        _userManager = userManager;
        _storage = storage;
        _activityLog = activityLog;
        _notifications = notifications;
        _logger = logger;
    }

    // GET: /GuestCheckIn?bookingId=5
    [HttpGet]
    public async Task<IActionResult> Index(int bookingId)
    {
        var model = await BuildModelAsync(bookingId);

        if (model is null)
            return NotFound();

        return View(model);
    }

    // POST: /GuestCheckIn/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        GuestCheckInViewModel model)
    {
        var guestId = _userManager.GetUserId(User);

        if (guestId is null)
            return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
                .ThenInclude(p => p.CheckInProcess)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(b =>
                b.Id == model.BookingId &&
                b.GuestId == guestId);

        if (booking is null)
            return NotFound();

        if (booking.Status != BookingStatus.Approved &&
            booking.Status != BookingStatus.CheckedIn)
        {
            TempData["Error"] =
                "Your booking must be approved before check-in.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        var process = booking.Property.CheckInProcess;

        if (process is null)
        {
            TempData["Error"] =
                "The Host has not configured check-in requirements.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        var requiredDocuments = process.RequiredDocuments
            .Split(',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (!requiredDocuments.Contains(
            model.DocumentType,
            StringComparer.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Invalid document type.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        if (model.DocumentFile is null)
        {
            TempData["Error"] = "Select a document.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        var error = await _storage.ValidateAsync(
            model.DocumentFile);

        if (error is not null)
        {
            TempData["Error"] = error;

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        var checkIn = booking.GuestCheckIn;

        if (checkIn is not null &&
            (checkIn.Status == CheckInStatus.Submitted ||
             checkIn.Status == CheckInStatus.Verified))
        {
            TempData["Error"] =
                "This check-in has already been submitted.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        if (checkIn is null)
        {
            checkIn = new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = process.Id,
                Status = CheckInStatus.InProgress,
                CurrentStep = 1
            };

            _context.GuestCheckIns.Add(checkIn);
        }

        // Check whether this document type was uploaded before.
        var existingDocument = checkIn.Documents
            .FirstOrDefault(d =>
                d.DocumentType.Equals(
                    model.DocumentType,
                    StringComparison.OrdinalIgnoreCase));

        // Only rejected documents may be replaced.
        if (existingDocument is not null &&
            existingDocument.Status != DocumentStatus.Rejected)
        {
            TempData["Error"] =
                "This document has already been uploaded. " +
                "You can replace it only if the Host rejects it.";

            return RedirectToAction(nameof(Index),
                new { bookingId = booking.Id });
        }

        // Remember the previous filename before replacing it.
        string? oldFileName = existingDocument?.FileName;

        // Save the new document to private storage.
        var storedFileName =
            await _storage.SaveAsync(model.DocumentFile);

        if (existingDocument is not null)
        {
            existingDocument.FileName = storedFileName;
            existingDocument.Status = DocumentStatus.Pending;
            existingDocument.HostRemarks = null;
            existingDocument.UploadedAt = DateTime.UtcNow;
        }
        else
        {
            checkIn.Documents.Add(new GuestDocument
            {
                DocumentType = model.DocumentType,
                FileName = storedFileName,
                Status = DocumentStatus.Pending,
                UploadedAt = DateTime.UtcNow
            });
        }

        checkIn.Status = CheckInStatus.InProgress;
        checkIn.CompletedAt = null;

        // Save the database changes before deleting the old file.
        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // The database save failed.
            // Remove the newly uploaded file to avoid leaving
            // an unnecessary private document on disk.
            try
            {
                await _storage.DeleteAsync(storedFileName);
            }
            catch (Exception cleanupException)
            {
                _logger.LogWarning(
                    cleanupException,
                    "Failed to clean up new document {FileName} " +
                    "after a database save error.",
                    storedFileName);
            }

            throw;
        }

        // The database now references the replacement document.
        // It is safe to try deleting the old rejected file.
        if (!string.IsNullOrWhiteSpace(oldFileName) &&
            oldFileName != storedFileName)
        {
            try
            {
                await _storage.DeleteAsync(oldFileName);
            }
            catch (Exception cleanupException)
            {
                // Do not undo a successful upload because
                // the old file could not be deleted.
                _logger.LogWarning(
                    cleanupException,
                    "Failed to delete replaced document {FileName}.",
                    oldFileName);
            }
        }

        TempData["Success"] =
            "Document uploaded successfully.";

        return RedirectToAction(nameof(Index),
            new { bookingId = booking.Id });
    }

    // POST: /GuestCheckIn/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int bookingId)
    {
        var guestId = _userManager.GetUserId(User);

        if (guestId is null)
            return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
                .ThenInclude(p => p.CheckInProcess)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.GuestId == guestId);

        if (booking is null)
            return NotFound();

        if (booking.Status != BookingStatus.Approved &&
            booking.Status != BookingStatus.CheckedIn)
        {
            return BadRequest("Booking is not approved.");
        }

        var checkIn = booking.GuestCheckIn;
        var process = booking.Property.CheckInProcess;

        if (checkIn is null ||
            process is null ||
            checkIn.CheckInProcessId != process.Id)
        {
            return BadRequest("Check-in is not ready.");
        }

        if (checkIn.Status != CheckInStatus.InProgress &&
            checkIn.Status != CheckInStatus.Rejected)
        {
            return BadRequest(
                "Check-in cannot be submitted.");
        }

        var required = process.RequiredDocuments
            .Split(',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        var hasAllDocuments = required.All(type =>
            checkIn.Documents.Any(d =>
                d.DocumentType.Equals(
                    type,
                    StringComparison.OrdinalIgnoreCase) &&
                d.Status != DocumentStatus.Rejected));

        if (!hasAllDocuments)
        {
            TempData["Error"] =
                "Upload all required documents before submitting.";

            return RedirectToAction(nameof(Index),
                new { bookingId });
        }

        checkIn.Status = CheckInStatus.Submitted;
        checkIn.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            guestId,
            "GuestCheckInSubmitted",
            ActivityType.CheckIn,
            $"Guest submitted check-in for booking #{bookingId}.");

        await _notifications.CreateAsync(
            booking.Property.HostId,
            "Guest check-in submitted",
            $"A guest submitted documents for booking #{bookingId}.",
            NotificationType.CheckIn,
            "/Host/GuestCheckIns");

        TempData["Success"] =
            "Your documents were submitted for Host verification.";

        return RedirectToAction(nameof(Index),
            new { bookingId });
    }

    // Build the Guest check-in page model.
    private async Task<GuestCheckInViewModel?> BuildModelAsync(
        int bookingId)
    {
        var guestId = _userManager.GetUserId(User);

        if (guestId is null)
            return null;

        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
                .ThenInclude(p => p.CheckInProcess)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.Documents)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.GuestId == guestId);

        if (booking is null)
            return null;

        if (booking.Status != BookingStatus.Approved &&
            booking.Status != BookingStatus.CheckedIn)
        {
            return null;
        }

        var process = booking.Property.CheckInProcess;

        if (process is null)
            return null;

        List<string> steps;

        try
        {
            steps = JsonSerializer.Deserialize<List<string>>(
                process.StepsJson) ?? new();
        }
        catch (JsonException)
        {
            steps = new();
        }

        return new GuestCheckInViewModel
        {
            BookingId = booking.Id,
            PropertyTitle = booking.Property.Title,
            CheckInTitle = process.Title,
            Steps = steps,

            RequiredDocuments = process.RequiredDocuments
                .Split(',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .ToList(),

            Status = booking.GuestCheckIn?.Status ??
                     CheckInStatus.Pending,

            Documents = booking.GuestCheckIn?.Documents
                .Select(d => new GuestCheckInDocumentViewModel
                {
                    DocumentType = d.DocumentType,
                    Status = d.Status,
                    HostRemarks = d.HostRemarks
                })
                .ToList() ?? new()
        };
    }
}

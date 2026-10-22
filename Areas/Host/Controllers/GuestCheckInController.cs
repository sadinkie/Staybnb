
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
public class GuestCheckInsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPrivateDocumentStorageService _storage;
    private readonly IActivityLogService _activityLog;
    private readonly INotificationService _notifications;

    public GuestCheckInsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPrivateDocumentStorageService storage,
        IActivityLogService activityLog,
        INotificationService notifications)
    {
        _context = context;
        _userManager = userManager;
        _storage = storage;
        _activityLog = activityLog;
        _notifications = notifications;
    }

    // GET: /Host/GuestCheckIns
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var checkIns = await _context.GuestCheckIns
            .AsNoTracking()
            .Include(c => c.Booking)
                .ThenInclude(b => b.Property)
            .Include(c => c.Booking)
                .ThenInclude(b => b.Guest)
            .Include(c => c.Documents)
            .Where(c => c.Booking.Property.HostId == hostId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(checkIns);
    }

    // GET: /Host/GuestCheckIns/Document/5
    [HttpGet]
    public async Task<IActionResult> Document(int id)
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var document = await _context.GuestDocuments
            .AsNoTracking()
            .Include(d => d.GuestCheckIn)
                .ThenInclude(c => c.Booking)
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                d.GuestCheckIn.Booking.Property.HostId == hostId);

        if (document is null)
            return NotFound();

        Stream stream;

        try
        {
            stream = await _storage.OpenReadAsync(
                document.FileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }

        var extension = Path.GetExtension(
            document.FileName).ToLowerInvariant();

        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(
            stream,
            contentType,
            $"GuestDocument-{document.Id}{extension}");
    }

    // POST: /Host/GuestCheckIns/ReviewDocument
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewDocument(
        int documentId,
        bool approve,
        string? remarks)
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var document = await _context.GuestDocuments
            .Include(d => d.GuestCheckIn)
                .ThenInclude(c => c.Booking)
                    .ThenInclude(b => b.Property)
            .FirstOrDefaultAsync(d =>
                d.Id == documentId &&
                d.GuestCheckIn.Booking.Property.HostId == hostId);

        if (document is null)
            return NotFound();

        var checkIn = document.GuestCheckIn;

        if (checkIn.Status != CheckInStatus.Submitted)
        {
            TempData["Error"] =
                "Only submitted check-ins can be reviewed.";

            return RedirectToAction(nameof(Index));
        }

        if (document.Status != DocumentStatus.Pending)
        {
            TempData["Error"] =
                "This document has already been reviewed.";

            return RedirectToAction(nameof(Index));
        }

        if (!approve && string.IsNullOrWhiteSpace(remarks))
        {
            TempData["Error"] =
                "Please provide a reason for rejection.";

            return RedirectToAction(nameof(Index));
        }

        if (remarks?.Length > 500)
        {
            TempData["Error"] =
                "Remarks cannot exceed 500 characters.";

            return RedirectToAction(nameof(Index));
        }

        // Load requirements before updating document status.
        var process = await _context.CheckInProcesses
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Id == checkIn.CheckInProcessId);

        if (process is null)
        {
            TempData["Error"] =
                "Check-in requirements could not be found.";

            return RedirectToAction(nameof(Index));
        }

        // Load all documents belonging to this check-in.
        await _context.Entry(checkIn)
            .Collection(c => c.Documents)
            .LoadAsync();

        // Update the document being reviewed.
        document.Status = approve
            ? DocumentStatus.Verified
            : DocumentStatus.Rejected;

        document.HostRemarks =
            string.IsNullOrWhiteSpace(remarks)
                ? null
                : remarks.Trim();

        // Get the documents required by the Host.
        var requiredDocuments = process.RequiredDocuments
            .Split(',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        // Check whether any document was rejected.
        var hasRejectedDocuments = checkIn.Documents.Any(d =>
            d.Status == DocumentStatus.Rejected);

        // Every required document type must have a verified document.
        var allRequiredVerified =
            requiredDocuments.Length > 0 &&
            requiredDocuments.All(type =>
                checkIn.Documents.Any(d =>
                    d.DocumentType.Equals(
                        type,
                        StringComparison.OrdinalIgnoreCase) &&
                    d.Status == DocumentStatus.Verified));

        // Also ensure there are no outstanding pending documents.
        var allUploadedVerified =
            checkIn.Documents.Any() &&
            checkIn.Documents.All(d =>
                d.Status == DocumentStatus.Verified);

        // Calculate the overall Guest check-in status.
        if (hasRejectedDocuments)
        {
            checkIn.Status = CheckInStatus.Rejected;
        }
        else if (allRequiredVerified && allUploadedVerified)
        {
            checkIn.Status = CheckInStatus.Verified;
        }
        else
        {
            checkIn.Status = CheckInStatus.Submitted;
        }

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            hostId,
            approve
                ? "GuestDocumentVerified"
                : "GuestDocumentRejected",
            ActivityType.CheckIn,
            $"Document #{document.Id} reviewed for booking " +
            $"#{checkIn.BookingId}.");

        await _notifications.CreateAsync(
            checkIn.Booking.GuestId,
            approve
                ? "Document verified"
                : "Document rejected",
            approve
                ? "Your identification document was verified."
                : $"Your document was rejected: {document.HostRemarks}",
            NotificationType.CheckIn,
            $"/GuestCheckIn?bookingId={checkIn.BookingId}");

        TempData["Success"] =
            "Document review saved successfully.";

        return RedirectToAction(nameof(Index));
    }
}

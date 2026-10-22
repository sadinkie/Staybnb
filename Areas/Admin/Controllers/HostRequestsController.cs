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
public class HostRequestsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHostApplicationReviewService _review;

    public HostRequestsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IHostApplicationReviewService review)
    {
        _context = context;
        _userManager = userManager;
        _review = review;
    }

    // status = Pending (default) | Approved | Rejected | All
    public async Task<IActionResult> Index(string? status)
    {
        ApplicationStatus? filter = ApplicationStatus.Pending;

        if (string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            filter = null;
        else if (Enum.TryParse<ApplicationStatus>(status, ignoreCase: true, out var parsed))
            filter = parsed;

        var query = _context.HostApplications.AsNoTracking();

        if (filter.HasValue)
            query = query.Where(a => a.Status == filter.Value);

        var model = new HostRequestsViewModel
        {
            Filter = filter,
            Applications = await query
                .OrderByDescending(a => a.AppliedAt)
                .ToListItems()
                .ToListAsync(),
            PendingCount = await _context.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
            ApprovedCount = await _context.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Approved),
            RejectedCount = await _context.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Rejected)
        };

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _context.HostApplications
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new HostRequestDetailsViewModel
            {
                ApplicationId = a.Id,
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                ReviewedAt = a.ReviewedAt,
                AdminNotes = a.AdminNotes,

                ApplicantName = a.ApplicationUser.FirstName + " " + a.ApplicationUser.LastName,
                ApplicantEmail = a.ApplicationUser.Email,
                ApplicantJoined = a.ApplicationUser.CreatedAt,
                IsAdditional = a.ApplicationUser.HostApplications
                    .Any(x => x.AppliedAt < a.AppliedAt && x.Status == ApplicationStatus.Approved),

                PropertyId = a.PropertyId,
                Title = a.Property.Title,
                Description = a.Property.Description,
                Address = a.Property.Address,
                City = a.Property.City,
                Country = a.Property.Country,
                PropertyType = a.Property.PropertyType,
                PricePerNight = a.Property.PricePerNight,
                CleaningFee = a.Property.CleaningFee,
                MaxGuests = a.Property.MaxGuests,
                Bedrooms = a.Property.Bedrooms,
                Beds = a.Property.Beds,
                Bathrooms = a.Property.Bathrooms,

                Images = a.Property.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).ToList(),
                Amenities = a.Property.Amenities.OrderBy(m => m.Name).ToList()
            })
            .FirstOrDefaultAsync();

        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        var result = await _review.ApproveAsync(id, admin);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? notes)
    {
        var admin = await _userManager.GetUserAsync(User);
        if (admin is null) return Challenge();

        var result = await _review.RejectAsync(id, admin, notes);

        if (!result.Succeeded)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
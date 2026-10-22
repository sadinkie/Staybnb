using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = RoleNames.Host)]
public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPropertyApplicationService _applications;
    private readonly IPropertyManagementService _management;

    public PropertiesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IPropertyApplicationService applications,
        IPropertyManagementService management)
    {
        _context = context;
        _userManager = userManager;
        _applications = applications;
        _management = management;
    }

    // ---------------- MY PROPERTIES ----------------

    public async Task<IActionResult> Index()
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        var items = await _context.HostProperties
            .AsNoTracking()
            .Where(p => p.HostId == host.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new HostPropertyListItem
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                Country = p.Country,
                PricePerNight = p.PricePerNight,
                PropertyType = p.PropertyType,
                MaxGuests = p.MaxGuests,
                Bedrooms = p.Bedrooms,
                Beds = p.Beds,
                Bathrooms = p.Bathrooms,
                ThumbnailUrl = p.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
                ImageCount = p.Images.Count(),
                IsApproved = p.IsApproved,
                IsActive = p.IsActive,
                LatestApplicationStatus = _context.HostApplications
                    .Where(a => a.PropertyId == p.Id)
                    .OrderByDescending(a => a.AppliedAt)
                    .Select(a => (ApplicationStatus?)a.Status)
                    .FirstOrDefault(),
                AdminNotes = _context.HostApplications
                    .Where(a => a.PropertyId == p.Id)
                    .OrderByDescending(a => a.AppliedAt)
                    .Select(a => a.AdminNotes)
                    .FirstOrDefault(),
                BookingCount = p.Bookings.Count(),
                ReviewCount = p.Reviews.Count(),
                AverageRating = p.Reviews.Average(r => (double?)r.Rating) ?? 0
            })
            .ToListAsync();

        return View(items);
    }

    // ---------------- ACTIVATE / DEACTIVATE ----------------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        var result = await _management.ToggleActiveAsync(id, host);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ---------------- ADD NEW PROPERTY (additional application) ----------------

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await WithAmenitiesAsync(new BecomeHostViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRules.MaxRequestBytes)]
    public async Task<IActionResult> Create(BecomeHostViewModel model)
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        foreach (var error in await _applications.ValidateImagesAsync(model.Images))
            ModelState.AddModelError(nameof(model.Images), error);

        if (!ModelState.IsValid)
            return View(await WithAmenitiesAsync(model));

        // Same service as "Become a Host", flagged as an additional property
        var result = await _applications.SubmitAsync(host, model, isAdditional: true);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(model.Images), error);

            return View(await WithAmenitiesAsync(model));
        }

        TempData["Success"] = "Property submitted! An Admin will review it before it goes live.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- EDIT ----------------

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        var model = await _management.GetForEditAsync(id, host.Id);
        if (model is null) return NotFound();

        model.AvailableAmenities = await LoadAmenitiesAsync();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRules.MaxRequestBytes)]
    public async Task<IActionResult> Edit(EditPropertyViewModel model)
    {
        var host = await _userManager.GetUserAsync(User);
        if (host is null) return Challenge();

        if (!ModelState.IsValid)
            return await RedisplayEditAsync(model, host);

        var errors = await _management.UpdateAsync(host, model);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);

            return await RedisplayEditAsync(model, host);
        }

        TempData["Success"] = "Your property was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------- HELPERS ----------------

    private async Task<IActionResult> RedisplayEditAsync(EditPropertyViewModel model, ApplicationUser host)
    {
        // The photo list is not posted back, so reload it from the database
        var current = await _management.GetForEditAsync(model.Id, host.Id);
        if (current is null) return NotFound();

        model.ExistingImages = current.ExistingImages;
        model.AvailableAmenities = await LoadAmenitiesAsync();
        return View(model);
    }

    private async Task<BecomeHostViewModel> WithAmenitiesAsync(BecomeHostViewModel model)
    {
        model.AvailableAmenities = await LoadAmenitiesAsync();
        return model;
    }

    private Task<List<Amenity>> LoadAmenitiesAsync() =>
        _context.Amenities.AsNoTracking().OrderBy(a => a.Name).ToListAsync();
}
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
public class BecomeHostController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IPropertyApplicationService _applications;

    public BecomeHostController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IPropertyApplicationService applications)
    {
        _userManager = userManager;
        _context = context;
        _applications = applications;
    }

    // ---------------- APPLICATION FORM ----------------

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (await HasPendingApplicationAsync(user.Id))
        {
            TempData["Info"] = "You already have an application under review.";
            return RedirectToAction(nameof(Status));
        }

        return View(await BuildFormAsync(new BecomeHostViewModel()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(UploadRules.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadRules.MaxRequestBytes)]
    public async Task<IActionResult> Index(BecomeHostViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        if (await HasPendingApplicationAsync(user.Id))
        {
            TempData["Info"] = "You already have an application under review.";
            return RedirectToAction(nameof(Status));
        }

        // Photo rules are checked even when other fields are wrong, so the user sees every problem at once
        foreach (var error in await _applications.ValidateImagesAsync(model.Images))
            ModelState.AddModelError(nameof(model.Images), error);

        if (!ModelState.IsValid)
            return View(await BuildFormAsync(model));

        var result = await _applications.SubmitAsync(user, model);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(model.Images), error);

            return View(await BuildFormAsync(model));
        }

        TempData["Success"] = "Application submitted! An Admin will review it shortly.";
        return RedirectToAction(nameof(Status));
    }

    // ---------------- STATUS ----------------

    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var items = await _context.HostApplications
            .AsNoTracking()
            .Where(a => a.ApplicationUserId == user.Id)
            .OrderByDescending(a => a.AppliedAt)
            .Select(a => new HostApplicationStatusItem
            {
                ApplicationId = a.Id,
                PropertyTitle = a.Property.Title,
                City = a.Property.City,
                PricePerNight = a.Property.PricePerNight,
                ThumbnailUrl = a.Property.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault(),
                Status = a.Status,
                AppliedAt = a.AppliedAt,
                ReviewedAt = a.ReviewedAt,
                AdminNotes = a.AdminNotes
            })
            .ToListAsync();

        return View(new HostApplicationStatusViewModel
        {
            Applications = items,
            CanApply = items.All(i => i.Status != ApplicationStatus.Pending)
        });
    }

    // ---------------- HELPERS ----------------

    private Task<bool> HasPendingApplicationAsync(string userId) =>
        _context.HostApplications.AnyAsync(a =>
            a.ApplicationUserId == userId && a.Status == ApplicationStatus.Pending);

    private async Task<BecomeHostViewModel> BuildFormAsync(BecomeHostViewModel model)
    {
        model.AvailableAmenities = await _context.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync();

        return model;
    }
}
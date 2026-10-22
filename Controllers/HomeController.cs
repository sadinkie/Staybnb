using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? city, PropertyType? type)
    {
        // The gallery is a Guest-facing page. Hosts and Admins work from their own dashboards.
        if (User.IsInRole(RoleNames.Host))
            return RedirectToAction("Index", "Dashboard", new { area = "Host" });

        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.SuperAdmin))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        // Only approved AND active listings are ever public
        var active = _context.HostProperties
            .AsNoTracking()
            .Where(p => p.IsApproved && p.IsActive);

        var filtered = active;

        if (!string.IsNullOrWhiteSpace(city))
            filtered = filtered.Where(p => p.City == city);

        if (type.HasValue)
            filtered = filtered.Where(p => p.PropertyType == type.Value);

        var properties = await filtered
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PropertyCardViewModel
            {
                Id = p.Id,
                Title = p.Title,
                City = p.City,
                Country = p.Country,
                PricePerNight = p.PricePerNight,
                PropertyType = p.PropertyType,
                MaxGuests = p.MaxGuests,
                ImageUrls = p.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).ToList(),
                ReviewCount = p.Reviews.Count(),
                AverageRating = p.Reviews.Average(r => (double?)r.Rating) ?? 0
            })
            .ToListAsync();

        var model = new PropertyGalleryViewModel
        {
            Properties = properties,
            City = city,
            Type = type,
            Cities = await active.Select(p => p.City).Distinct().OrderBy(c => c).ToListAsync(),
            AvailableTypes = (await active.Select(p => p.PropertyType).Distinct().ToListAsync())
                .OrderBy(t => t)
                .ToList()
        };

        return View("Index1", model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
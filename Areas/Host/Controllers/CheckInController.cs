
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

namespace Staybnb.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = RoleNames.Host)]
public class CheckInController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogService _activityLog;

    public CheckInController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLog)
    {
        _context = context;
        _userManager = userManager;
        _activityLog = activityLog;
    }

    // GET: /Host/CheckIn
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var properties = await _context.HostProperties
            .AsNoTracking()
            .Include(p => p.CheckInProcess)
            .Where(p => p.HostId == hostId && p.IsApproved)
            .OrderBy(p => p.Title)
            .ToListAsync();

        return View(properties);
    }

    // GET: /Host/CheckIn/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var property = await _context.HostProperties
            .AsNoTracking()
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.HostId == hostId &&
                p.IsApproved);

        if (property is null) return NotFound();

        var process = property.CheckInProcess;

        var documents = (process?.RequiredDocuments ?? "ID")
            .Split(',', StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

        var instructions = string.Empty;

        if (process is not null)
        {
            try
            {
                var steps = JsonSerializer.Deserialize<List<string>>(
                    process.StepsJson);

                instructions = string.Join(
                    Environment.NewLine,
                    steps ?? new List<string>());
            }
            catch (JsonException)
            {
                instructions = string.Empty;
            }
        }

        var model = new HostCheckInViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            Title = process?.Title ?? "Guest Check-in",
            Instructions = instructions,
            RequireId = documents.Contains(
                "ID", StringComparer.OrdinalIgnoreCase),
            RequirePassport = documents.Contains(
                "Passport", StringComparer.OrdinalIgnoreCase)
        };

        return View(model);
    }

    // POST: /Host/CheckIn/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, HostCheckInViewModel model)
    {
        if (id != model.PropertyId) return BadRequest();

        var hostId = _userManager.GetUserId(User);
        if (hostId is null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.HostId == hostId &&
                p.IsApproved);

        if (property is null) return NotFound();

        model.PropertyTitle = property.Title;

        if (!model.RequireId && !model.RequirePassport)
        {
            ModelState.AddModelError(
                string.Empty,
                "Select at least one required document.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var steps = model.Instructions
            .Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .ToList();

        var documents = new List<string>();

        if (model.RequireId)
            documents.Add("ID");

        if (model.RequirePassport)
            documents.Add("Passport");

        var process = property.CheckInProcess;

        if (process is null)
        {
            process = new CheckInProcess
            {
                PropertyId = property.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.CheckInProcesses.Add(process);
        }

        process.Title = model.Title.Trim();
        process.StepsJson = JsonSerializer.Serialize(steps);
        process.RequiredDocuments = string.Join(",", documents);

        await _context.SaveChangesAsync();

        await _activityLog.LogAsync(
            hostId,
            "CheckInRequirementsUpdated",
            ActivityType.CheckIn,
            $"Host updated check-in requirements for property #{property.Id}.");

        TempData["Success"] =
            "Check-in requirements saved successfully.";

        return RedirectToAction(nameof(Index));
    }
}

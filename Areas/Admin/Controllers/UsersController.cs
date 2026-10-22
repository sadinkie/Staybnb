using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Areas.Admin.Controllers;

// User management is for the SuperAdmin ONLY
[Area("Admin")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserPromotionService _promotion;

    public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserPromotionService promotion)
    {
        _context = context;
        _userManager = userManager;
        _promotion = promotion;
    }

    public async Task<IActionResult> Index(string? search, string role = "All")
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FirstName.Contains(term)
                                  || u.LastName.Contains(term)
                                  || u.Email!.Contains(term));
        }

        if (role != "All" && RoleNames.All.Contains(role))
        {
            query = query.Where(u => _context.UserRoles.Any(ur =>
                ur.UserId == u.Id &&
                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == role)));
        }

        var users = await query
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(200)
            .ToListAsync();

        // Load the roles for just these users in one query
        var ids = users.Select(u => u.Id).ToList();
        var roleRows = await (
            from ur in _context.UserRoles
            where ids.Contains(ur.UserId)
            join r in _context.Roles on ur.RoleId equals r.Id
            select new { ur.UserId, RoleName = r.Name }
        ).ToListAsync();

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName ?? "").OrderBy(n => n).ToList());

        var model = new UsersViewModel
        {
            Search = search,
            Role = role,
            Users = users.Select(u => new UserListItem
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                Roles = rolesByUser.TryGetValue(u.Id, out var list) ? list : new List<string>()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(string id, string? search, string role = "All")
    {
        var actor = await _userManager.GetUserAsync(User);
        if (actor is null) return Challenge();

        var result = await _promotion.PromoteGuestToAdminAsync(id, actor);

        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index), new { search, role });
    }
}
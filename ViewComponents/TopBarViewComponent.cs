using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.ViewComponents;

public class TopBarViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public TopBarViewComponent(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications)
    {
        _context = context;
        _userManager = userManager;
        _notifications = notifications;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
        if (user is null)
            return Content(string.Empty);

        var model = new TopBarViewModel
        {
            FullName = user.FullName,
            Initials = $"{user.FirstName.FirstOrDefault()}{user.LastName.FirstOrDefault()}".ToUpper(),
            RoleLabel = GetRoleLabel(),
            UnreadNotifications = await _notifications.GetUnreadCountAsync(user.Id),
            UnreadMessages = await _context.Messages
                .CountAsync(m => m.ReceiverId == user.Id && !m.IsRead),
            Recent = await _notifications.GetRecentAsync(user.Id, 5)
        };

        return View(model);
    }

    private string GetRoleLabel()
    {
        if (UserClaimsPrincipal.IsInRole(RoleNames.SuperAdmin)) return "Super Admin";
        if (UserClaimsPrincipal.IsInRole(RoleNames.Admin)) return "Administrator";
        if (UserClaimsPrincipal.IsInRole(RoleNames.Host)) return "Host";
        return "Guest";
    }
}
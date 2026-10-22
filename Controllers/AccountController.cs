using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Data;
using Staybnb.Enums;
using Staybnb.Models;
using Staybnb.Services;
using Staybnb.ViewModels;

namespace Staybnb.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IActivityLogService _activityLog;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IActivityLogService activityLog)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _activityLog = activityLog;
    }

    // ---------------- REGISTER ----------------

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "" });

        return View(new RegisterViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // Every new registrant starts as a Guest
        await _userManager.AddToRoleAsync(user, RoleNames.Guest);

        await _activityLog.LogAsync(user.Id, "UserRegistered", ActivityType.Registration,
            $"{user.Email} registered and was assigned the Guest role.");

        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToAction("Index", "Home", new { area = "" });
    }

    // ---------------- LOGIN ----------------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home", new { area = "" });

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _activityLog.LogAsync(user.Id, "UserLoggedIn", ActivityType.Login,
                $"{user.Email} logged in.");

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return await RedirectByRoleAsync(user);
        }

        if (result.IsLockedOut)
        {
            await _activityLog.LogAsync(user.Id, "AccountLockedOut", ActivityType.Login,
                $"{user.Email} was locked out after repeated failed logins.");

            ModelState.AddModelError(string.Empty,
                "Your account is temporarily locked after too many failed attempts. Try again in a few minutes.");
        }
        else
        {
            await _activityLog.LogAsync(user.Id, "LoginFailed", ActivityType.Login,
                $"Failed login attempt for {user.Email}.");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
        }

        return View(model);
    }

    // ---------------- LOGOUT ----------------

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);

        await _signInManager.SignOutAsync();

        if (user is not null)
        {
            await _activityLog.LogAsync(user.Id, "UserLoggedOut", ActivityType.Logout,
                $"{user.Email} logged out.");
        }

        return RedirectToAction("Index", "Home", new { area = "" });
    }

    // ---------------- ACCESS DENIED ----------------

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ---------------- HELPERS ----------------

    /// <summary>Sends each role to its own home: Admins to /Admin, Hosts to /Host, Guests to the gallery.</summary>
    private async Task<IActionResult> RedirectByRoleAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains(RoleNames.SuperAdmin) || roles.Contains(RoleNames.Admin))
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

        if (roles.Contains(RoleNames.Host))
            return RedirectToAction("Index", "Dashboard", new { area = "Host" });

        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
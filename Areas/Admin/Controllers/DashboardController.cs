using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Staybnb.Data;

namespace Staybnb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = RoleNames.AdminOrSuperAdmin)]
public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
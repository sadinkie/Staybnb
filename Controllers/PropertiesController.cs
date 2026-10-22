
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staybnb.Data;
using Staybnb.Models;

namespace Staybnb.Controllers;

public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public PropertiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Properties/View/5
    [HttpGet]
    public async Task<IActionResult> View(int id)
    {
        var property = await _context.HostProperties
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p =>
                p.Id == id &&
                p.IsApproved &&
                p.IsActive);

        if (property is null)
            return NotFound();

        return View("Details", property);
    }
}

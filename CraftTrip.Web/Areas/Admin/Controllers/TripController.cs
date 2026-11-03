using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TripController : Controller
{
    private readonly CraftTripDbContext _context;

    public TripController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var trips = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Village)
            .Include(t => t.Workshop)
            .Include(t => t.Itineraries)
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        return View(trips);
    }
}

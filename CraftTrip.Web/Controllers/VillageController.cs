using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

public class VillageController : Controller
{
    private readonly CraftTripDbContext _context;

    public VillageController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var villages = await _context.Villages
            .AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return View(villages);
    }

    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();

        var village = await _context.Villages
            .AsNoTracking()
            .Include(v => v.Images.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(v => v.Slug == slug && !v.IsDeleted);

        if (village == null) return NotFound("Không tìm thấy thông tin làng nghề.");

        var workshops = await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Host)
            .Where(w => w.VillageId == village.Id && w.Status == Core.Enums.WorkshopStatus.Published && !w.IsDeleted)
            .ToListAsync();

        var artisans = await _context.ArtisanProfiles
            .AsNoTracking()
            .Where(a => a.VillageId == village.Id && !a.IsDeleted)
            .ToListAsync();

        var trips = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Itineraries.OrderBy(i => i.StopOrder))
            .Where(t => t.VillageId == village.Id && !t.IsDeleted)
            .ToListAsync();

        var viewModel = new VillageDetailViewModel
        {
            Village = village,
            Workshops = workshops,
            Artisans = artisans,
            Trips = trips
        };

        return View(viewModel);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Infrastructure.Data;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class WorkshopController : Controller
{
    private readonly CraftTripDbContext _context;

    public WorkshopController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var workshops = await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Host)
                .ThenInclude(h => h!.HostProfile)
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Where(w => !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return View(workshops);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(Guid id)
    {
        var workshop = await _context.Workshops.FindAsync(id);
        if (workshop != null)
        {
            workshop.IsFeatured = !workshop.IsFeatured;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = workshop.IsFeatured ? "Đã gắn nổi bật workshop!" : "Đã hủy nổi bật workshop.";
        }
        return RedirectToAction("Index");
    }
}

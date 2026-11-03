using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
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
            .Include(v => v.Workshops)
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return View(villages);
    }

    [HttpGet]
    public IActionResult Create() => View(new Village());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Village village)
    {
        if (!ModelState.IsValid) return View(village);

        village.Slug = GenerateSlug(village.Name);
        _context.Villages.Add(village);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã thêm làng nghề mới thành công!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var village = await _context.Villages.FindAsync(id);
        if (village == null || village.IsDeleted) return NotFound();
        return View(village);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Village model)
    {
        var village = await _context.Villages.FindAsync(id);
        if (village == null || village.IsDeleted) return NotFound();

        village.Name = model.Name.Trim();
        village.Location = model.Location.Trim();
        village.Address = model.Address.Trim();
        village.ShortDescription = model.ShortDescription.Trim();
        village.HistoryDescription = model.HistoryDescription.Trim();
        village.CraftProcessDescription = model.CraftProcessDescription.Trim();
        village.CoverImageUrl = model.CoverImageUrl;
        village.IsFeatured = model.IsFeatured;
        village.DisplayOrder = model.DisplayOrder;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật thông tin làng nghề!";
        return RedirectToAction("Index");
    }

    private static string GenerateSlug(string text)
    {
        return System.Text.RegularExpressions.Regex.Replace(text.ToLower().Trim(), @"[^a-z0-9\s-]", "")
            .Replace(" ", "-");
    }
}
